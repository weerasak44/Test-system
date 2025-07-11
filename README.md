# ระบบ POS (Point of Sale) 

ระบบขายหน้าร้านที่ครบครันพัฒนาด้วย C# Windows Forms และ Entity Framework

## ฟีเจอร์หลัก

### 🔐 ระบบหลัก (Core System)
- **หน้าลงชื่อเข้าใช้งาน**: การยืนยันตัวตนด้วย Username/Password
- **หน้าแดชบอร์ด**: ขายสินค้า ค้นหาสินค้า/ลูกค้า พร้อมตารางแสดงรายการขาย
- **ระบบควบคุมราคา**: ราคาปกติ/พนักงาน/ส่ง
- **วิธีชำระเงิน**: เงินสด/โอน/เครดิต
- **คีย์ลัด**: F5 (ชำระเงิน), F9 (ยกเลิกบิล)

### 👥 ระบบจัดการเบื้องหลัง (Management Systems)
- **จัดการผู้ใช้งาน**: Admin/Cashier พร้อมระบบสิทธิ์
- **จัดการสมาชิก/ลูกหนี้**: รับชำระหนี้ ดูประวัติ
- **จัดการสินค้า**: เพิ่ม/แก้ไข/ลบ สินค้า Import/Export Excel

### 📊 ระบบรายงานและใบเสร็จ (Reporting & Receipt)
- **รายงานยอดขาย**: ตามช่วงวันที่ พร้อมกำไร
- **รายงานสต็อก**: สินค้าใกล้หมด มูลค่าสต็อก
- **รายงานตรวจนับสต็อก**: ภาพรวมสต็อกทั้งหมด
- **ใบเสร็จ**: อัตโนมัติ พิมพ์ได้ บันทึกไฟล์

## ความต้องการระบบ

- Windows 10/11
- .NET 6.0 Runtime หรือใหม่กว่า
- SQL Server LocalDB (รวมใน Visual Studio)

## การติดตั้ง

### วิธีที่ 1: รันจาก Source Code
1. Clone หรือดาวน์โหลดโปรเจกต์
2. เปิด Command Prompt/Terminal ใน folder โปรเจกต์
3. รันคำสั่ง:
   ```bash
   dotnet restore
   dotnet build
   dotnet run
   ```

### วิธีที่ 2: ผ่าน Visual Studio
1. เปิดไฟล์ `POS_System.csproj` ใน Visual Studio
2. กด F5 หรือ Start Debugging

## การใช้งาน

### การเข้าสู่ระบบ
- **Username**: `admin`
- **Password**: `admin123`

### การขายสินค้า
1. ใช้ช่องค้นหาสินค้าโดยพิมพ์รหัสหรือชื่อสินค้า แล้วกด Enter
2. เลือกลูกค้า (ถ้ามี) ผ่านช่องค้นหาลูกค้า
3. เลือกระดับราคาและวิธีชำระเงิน
4. กด F5 หรือปุ่ม "ชำระเงิน"

### การจัดการสินค้า (Admin เท่านั้น)
- เข้าเมนู "จัดการระบบ" > "จัดการสินค้า"
- เพิ่ม/แก้ไข/ลบ ข้อมูลสินค้า
- Import/Export ข้อมูลจาก Excel

### การรายงาน
- เข้าเมนู "รายงาน"
- เลือกประเภทรายงานที่ต้องการ
- กำหนดช่วงวันที่ (หากจำเป็น)

## โครงสร้างโปรเจกต์

```
POS_System/
├── Models/              # Entity Models
│   ├── User.cs
│   ├── Product.cs
│   ├── Customer.cs
│   ├── Sale.cs
│   ├── SaleItem.cs
│   └── Payment.cs
├── Data/               # Database Context
│   └── POSDbContext.cs
├── Services/           # Business Logic
│   ├── AuthService.cs
│   ├── ProductService.cs
│   ├── CustomerService.cs
│   └── SaleService.cs
├── Forms/              # UI Forms
│   ├── LoginForm.cs
│   └── MainForm.cs
├── Program.cs          # Entry Point
├── App.config          # Configuration
└── POS_System.csproj   # Project File
```

## ฐานข้อมูล

ระบบใช้ SQL Server LocalDB โดยจะสร้างฐานข้อมูลอัตโนมัติเมื่อรันครั้งแรก

### ตารางหลัก
- **Users**: ข้อมูลผู้ใช้งาน
- **Products**: ข้อมูลสินค้า
- **Customers**: ข้อมูลลูกค้า/สมาชิก  
- **Sales**: ข้อมูลการขาย
- **SaleItems**: รายการสินค้าในแต่ละบิล
- **Payments**: ข้อมูลการชำระหนี้

## ข้อมูลตัวอย่าง

ระบบจะมีข้อมูลตัวอย่างเมื่อรันครั้งแรก:

### ผู้ใช้งาน
- Admin: admin/admin123

### สินค้า
- P001: ข้าวสาร 5 กิโลกรัม
- P002: น้ำมันพืช 1 ลิตร

### ลูกค้า
- C001: ลูกค้าทั่วไป

## การพัฒนาต่อ

### การเพิ่มฟีเจอร์ใหม่
1. สร้าง Model ใหม่ใน folder `Models/`
2. อัปเดต `POSDbContext.cs`
3. สร้าง Service ใน folder `Services/`
4. สร้าง Form ใน folder `Forms/`

### การ Customize
- แก้ไขข้อมูลร้านค้าใน `App.config`
- ปรับแต่ง UI ใน Forms
- เพิ่มรายงานใหม่ในเมนูรายงาน

## แก้ไขปัญหา

### ปัญหาฐานข้อมูล
หากมีปัญหาเกี่ยวกับฐานข้อมูล ให้ลบไฟล์ `.mdf` และ `.ldf` ใน folder `bin/Debug/` แล้วรันใหม่

### ปัญหา Dependencies
```bash
dotnet restore --force
dotnet clean
dotnet build
```

## License

MIT License - ดูไฟล์ LICENSE สำหรับรายละเอียด

## ติดต่อ

หากมีคำถามหรือต้องการความช่วยเหลือ กรุณาสร้าง Issue ใน Repository นี้
