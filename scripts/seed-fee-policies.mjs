// Seed fee policies vào Atlas (chạy 1 lần).
//
// Vì sao: phí ước tính (Driver) và tính phí check-out (Staff) đều trả 404
// "No active fee policy matches..." do KHÔNG có fee_policy nào khớp
// (building + vehicleType) thật của lượt gửi. Script này tạo bảng giá khớp
// cho MỌI cặp (building × vehicleType) đang tồn tại trong parking_main_db.
//
// Giá đã chốt: xe máy 10.000/lượt, ô tô 30.000/lượt (PerTurn) +
// phụ phí quá giờ cố định 1 lần (xe máy 5.000, ô tô 10.000) sau 24h.
//
// Chạy lại an toàn: upsert theo (BuildingId + VehicleTypeId + IsActive=true)
// nên không tạo trùng.
//
// Cách chạy:  node scripts/seed-fee-policies.mjs
//   (đọc connection string từ src/Services/Payment/Payment.API/appsettings.json,
//    không hard-code secret)

import { MongoClient, ObjectId, Decimal128 } from 'mongodb';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, '..');

// --- Đọc connection string + db name từ appsettings của Payment ---
const appsettingsPath = join(
  repoRoot,
  'src/Services/Payment/Payment.API/appsettings.json',
);
const appsettings = JSON.parse(readFileSync(appsettingsPath, 'utf8'));
const CONN = appsettings.MongoDbSettings.ConnectionString;
const PAYMENT_DB = appsettings.MongoDbSettings.DatabaseName; // parking_payment_db
const PARKING_DB = 'parking_main_db'; // nơi chứa buildings + vehicle_types

// --- Phân loại loại xe theo Name → giá lượt + phụ phí quá giờ ---
function normalize(s) {
  return (s || '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '') // bỏ dấu tiếng Việt
    .replace(/[^a-z0-9]/g, ' ')
    .trim();
}

// Trả về { base, overtime } theo tên loại xe.
function priceFor(vehicleTypeName) {
  const n = normalize(vehicleTypeName);
  const isMoto =
    n.includes('may') || n.includes('moto') || n.includes('2 banh') || n.includes('xe may');
  const isCar =
    n.includes('oto') || n.includes('o to') || n.includes('car') || n.includes('4 banh');

  if (isCar && !isMoto) return { base: 30000, overtime: 10000, kind: 'ô tô' };
  if (isMoto) return { base: 10000, overtime: 5000, kind: 'xe máy' };
  // Không nhận diện được → mặc định xe máy 10k, cảnh báo.
  return { base: 10000, overtime: 5000, kind: 'KHÔNG RÕ (mặc định xe máy)' };
}

async function main() {
  const client = new MongoClient(CONN);
  await client.connect();
  console.log('=== CONNECTED ===\n');

  const parking = client.db(PARKING_DB);
  const payment = client.db(PAYMENT_DB);

  const buildings = await parking
    .collection('buildings')
    .find({ IsActive: true })
    .toArray();
  const vehicleTypes = await parking
    .collection('vehicle_types')
    .find({ IsActive: true })
    .toArray();

  console.log(`Buildings: ${buildings.length}, VehicleTypes: ${vehicleTypes.length}`);
  if (buildings.length === 0 || vehicleTypes.length === 0) {
    console.log('⚠ Thiếu building hoặc vehicle type — không seed được. Dừng.');
    await client.close();
    return;
  }

  const feePolicies = payment.collection('fee_policies');
  const now = new Date();
  const effectiveFrom = new Date('2020-01-01T00:00:00Z'); // đủ xa để <= mọi CheckInTime

  let created = 0;
  let updated = 0;
  const summary = [];

  for (const b of buildings) {
    for (const vt of vehicleTypes) {
      const buildingId = b._id.toString();
      const vehicleTypeId = vt._id.toString();
      const { base, overtime, kind } = priceFor(vt.Name);

      // Doc khớp entity C# FeePolicy (PascalCase). Decimal → Decimal128.
      const doc = {
        BuildingId: buildingId,
        VehicleTypeId: vehicleTypeId,
        Name: `${vt.Name} - ${b.Name}`,
        PricingType: 1, // PerTurn
        BasePrice: Decimal128.fromString(String(base)),
        HourlyPrice: null,
        DailyPrice: null,
        MonthlyPrice: null,
        LostTicketFee: Decimal128.fromString('50000'),
        OvertimeFee: Decimal128.fromString(String(overtime)),
        OvertimeAfterHours: 24,
        EffectiveFrom: effectiveFrom,
        EffectiveTo: null,
        IsActive: true,
        CreatedAt: now,
        UpdatedAt: null,
      };

      // Upsert theo cặp building+vehicleType đang active → không tạo trùng.
      const res = await feePolicies.updateOne(
        { BuildingId: buildingId, VehicleTypeId: vehicleTypeId, IsActive: true },
        {
          $set: doc,
          $setOnInsert: { _id: new ObjectId() },
        },
        { upsert: true },
      );

      if (res.upsertedCount > 0) created++;
      else updated++;

      summary.push(
        `  [${kind}] ${b.Name} × ${vt.Name} → ${base.toLocaleString('vi-VN')}đ/lượt, quá giờ +${overtime.toLocaleString('vi-VN')}đ`,
      );
    }
  }

  console.log('\n=== ĐÃ SEED ===');
  console.log(summary.join('\n'));
  console.log(`\nTạo mới: ${created} | Cập nhật: ${updated} | Tổng: ${created + updated}`);

  await client.close();
  console.log('\n=== DONE ===');
}

main().catch((e) => {
  console.error('LỖI:', e);
  process.exit(1);
});
