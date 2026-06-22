// Thăm dò dữ liệu thật trên Atlas trước khi seed fee policies.
// Đọc connection string từ Payment appsettings.json (không hard-code secret).
import { MongoClient } from 'mongodb';
import { readFileSync } from 'fs';

const appsettingsPath =
  '../src/Services/Payment/Payment.API/appsettings.json';
const cfg = JSON.parse(readFileSync(new URL(appsettingsPath, import.meta.url)));
const conn = cfg.MongoDbSettings.ConnectionString;

const MAIN_DB = 'parking_main_db';
const PAY_DB = 'parking_payment_db';

const client = new MongoClient(conn);

try {
  await client.connect();
  console.log('=== CONNECTED ===');

  const main = client.db(MAIN_DB);
  const pay = client.db(PAY_DB);

  // Buildings
  const buildings = await main.collection('buildings').find({}).toArray();
  console.log(`\n=== BUILDINGS (${buildings.length}) ===`);
  for (const b of buildings) {
    console.log(`  _id=${b._id} (${typeof b._id}) | Name=${b.Name} | IsActive=${b.IsActive}`);
  }

  // Vehicle types
  const vts = await main.collection('vehicle_types').find({}).toArray();
  console.log(`\n=== VEHICLE_TYPES (${vts.length}) ===`);
  for (const v of vts) {
    console.log(`  _id=${v._id} (${typeof v._id}) | Name=${v.Name} | IsActive=${v.IsActive}`);
  }

  // Fee policy template
  const fps = await pay.collection('fee_policies').find({}).limit(2).toArray();
  console.log(`\n=== FEE_POLICIES sample (${fps.length}) ===`);
  console.log(JSON.stringify(fps, null, 2));

  // Một số session active để biết building/vehicleType thật đang dùng
  const sessions = await main.collection('parking_sessions')
    .find({ Status: 1 }).limit(5).toArray();
  console.log(`\n=== ACTIVE SESSIONS (${sessions.length}) ===`);
  for (const s of sessions) {
    console.log(`  _id=${s._id} | Plate=${s.PlateNumber} | BuildingId=${s.BuildingId} | VehicleTypeId=${s.VehicleTypeId}`);
  }
} catch (e) {
  console.error('ERROR:', e.message);
} finally {
  await client.close();
}
