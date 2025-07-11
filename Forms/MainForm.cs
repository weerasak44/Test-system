using POS_System.Models;
using POS_System.Services;

namespace POS_System.Forms
{
    public partial class MainForm : Form
    {
        private readonly AuthService _authService;
        private readonly ProductService _productService;
        private readonly CustomerService _customerService;
        private readonly SaleService _saleService;

        // Current sale data
        private List<SaleItem> _currentSaleItems;
        private Customer? _selectedCustomer;
        private PriceLevel _currentPriceLevel;

        // UI Controls
        private MenuStrip menuStrip;
        private Label lblStoreName, lblDateTime, lblUser;
        private TextBox txtProductSearch, txtCustomerSearch;
        private DataGridView dgvSaleItems;
        private ComboBox cbPriceLevel, cbPaymentMethod;
        private Label lblSubTotal, lblTotal;
        private Button btnPay, btnCancel;
        private Timer clockTimer;

        public MainForm()
        {
            _authService = new AuthService();
            _productService = new ProductService();
            _customerService = new CustomerService();
            _saleService = new SaleService();
            
            _currentSaleItems = new List<SaleItem>();
            _currentPriceLevel = PriceLevel.Normal;

            InitializeComponent();
            SetupEvents();
            UpdateDisplay();
            StartClock();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1200, 800);
            this.Text = "ระบบ POS - หน้าขาย";
            this.WindowState = FormWindowState.Maximized;
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            CreateMenuStrip();
            CreateHeaderPanel();
            CreateSearchPanel();
            CreateSaleItemsGrid();
            CreateControlPanel();
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip();

            // ระบบขาย
            var mnuSale = new ToolStripMenuItem("ระบบขาย");
            mnuSale.DropDownItems.Add("บิลใหม่", null, (s, e) => NewSale());
            mnuSale.DropDownItems.Add("-");
            mnuSale.DropDownItems.Add("ออกจากระบบ", null, (s, e) => Logout());

            // จัดการระบบ (Admin only)
            if (_authService.IsAdmin())
            {
                var mnuManage = new ToolStripMenuItem("จัดการระบบ");
                mnuManage.DropDownItems.Add("จัดการผู้ใช้งาน", null, (s, e) => OpenUserManagement());
                mnuManage.DropDownItems.Add("จัดการสินค้า", null, (s, e) => OpenProductManagement());
                mnuManage.DropDownItems.Add("จัดการสมาชิก", null, (s, e) => OpenCustomerManagement());
                menuStrip.Items.Add(mnuManage);
            }

            // รายงาน
            var mnuReport = new ToolStripMenuItem("รายงาน");
            mnuReport.DropDownItems.Add("รายงานยอดขาย", null, (s, e) => OpenSalesReport());
            mnuReport.DropDownItems.Add("รายงานสต็อก", null, (s, e) => OpenStockReport());

            menuStrip.Items.AddRange(new ToolStripItem[] { mnuSale, mnuReport });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void CreateHeaderPanel()
        {
            var headerPanel = new Panel
            {
                Location = new Point(0, 30),
                Size = new Size(this.Width, 60),
                BackColor = Color.DarkBlue,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblStoreName = new Label
            {
                Text = "ร้านค้าตัวอย่าง",
                Font = new Font("Tahoma", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                Size = new Size(300, 30)
            };

            lblDateTime = new Label
            {
                Font = new Font("Tahoma", 10),
                ForeColor = Color.White,
                Location = new Point(this.Width - 300, 10),
                Size = new Size(280, 20),
                TextAlign = ContentAlignment.TopRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            lblUser = new Label
            {
                Text = $"ผู้ใช้: {AuthService.CurrentUser?.FullName}",
                Font = new Font("Tahoma", 10),
                ForeColor = Color.White,
                Location = new Point(this.Width - 300, 30),
                Size = new Size(280, 20),
                TextAlign = ContentAlignment.TopRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            headerPanel.Controls.AddRange(new Control[] { lblStoreName, lblDateTime, lblUser });
            this.Controls.Add(headerPanel);
        }

        private void CreateSearchPanel()
        {
            var searchPanel = new Panel
            {
                Location = new Point(10, 100),
                Size = new Size(this.Width - 20, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // Product search
            var lblProductSearch = new Label
            {
                Text = "ค้นหาสินค้า:",
                Location = new Point(10, 15),
                Size = new Size(80, 23)
            };

            txtProductSearch = new TextBox
            {
                Location = new Point(90, 15),
                Size = new Size(300, 23),
                Font = new Font("Tahoma", 10)
            };

            // Customer search
            var lblCustomerSearch = new Label
            {
                Text = "ค้นหาลูกค้า:",
                Location = new Point(10, 45),
                Size = new Size(80, 23)
            };

            txtCustomerSearch = new TextBox
            {
                Location = new Point(90, 45),
                Size = new Size(300, 23),
                Font = new Font("Tahoma", 10)
            };

            searchPanel.Controls.AddRange(new Control[] {
                lblProductSearch, txtProductSearch, lblCustomerSearch, txtCustomerSearch
            });

            this.Controls.Add(searchPanel);
        }

        private void CreateSaleItemsGrid()
        {
            dgvSaleItems = new DataGridView
            {
                Location = new Point(10, 190),
                Size = new Size(this.Width - 330, this.Height - 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White
            };

            dgvSaleItems.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "ProductCode", HeaderText = "รหัสสินค้า", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "ชื่อสินค้า", Width = 250 },
                new DataGridViewTextBoxColumn { Name = "Unit", HeaderText = "หน่วย", Width = 60 },
                new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "จำนวน", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "ราคา/หน่วย", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "TotalPrice", HeaderText = "รวม", Width = 100 }
            });

            this.Controls.Add(dgvSaleItems);
        }

        private void CreateControlPanel()
        {
            var controlPanel = new Panel
            {
                Location = new Point(this.Width - 310, 190),
                Size = new Size(300, this.Height - 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Price Level
            var lblPriceLevel = new Label
            {
                Text = "ระดับราคา:",
                Location = new Point(10, 20),
                Size = new Size(80, 23)
            };

            cbPriceLevel = new ComboBox
            {
                Location = new Point(90, 20),
                Size = new Size(180, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbPriceLevel.Items.AddRange(new object[] { "ปกติ", "พนักงาน", "ส่ง" });
            cbPriceLevel.SelectedIndex = 0;

            // Payment Method
            var lblPaymentMethod = new Label
            {
                Text = "วิธีชำระ:",
                Location = new Point(10, 60),
                Size = new Size(80, 23)
            };

            cbPaymentMethod = new ComboBox
            {
                Location = new Point(90, 60),
                Size = new Size(180, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbPaymentMethod.Items.AddRange(new object[] { "เงินสด", "โอน", "เครดิต" });
            cbPaymentMethod.SelectedIndex = 0;

            // Totals
            lblSubTotal = new Label
            {
                Text = "รวมย่อย: 0.00 บาท",
                Location = new Point(10, 120),
                Size = new Size(280, 23),
                Font = new Font("Tahoma", 10),
                TextAlign = ContentAlignment.MiddleRight
            };

            lblTotal = new Label
            {
                Text = "รวมทั้งหมด: 0.00 บาท",
                Location = new Point(10, 150),
                Size = new Size(280, 30),
                Font = new Font("Tahoma", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.Red
            };

            // Buttons
            btnPay = new Button
            {
                Text = "ชำระเงิน (F5)",
                Location = new Point(10, 200),
                Size = new Size(120, 40),
                BackColor = Color.Green,
                ForeColor = Color.White,
                Font = new Font("Tahoma", 10, FontStyle.Bold)
            };

            btnCancel = new Button
            {
                Text = "ยกเลิกบิล (F9)",
                Location = new Point(150, 200),
                Size = new Size(120, 40),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("Tahoma", 10, FontStyle.Bold)
            };

            controlPanel.Controls.AddRange(new Control[] {
                lblPriceLevel, cbPriceLevel, lblPaymentMethod, cbPaymentMethod,
                lblSubTotal, lblTotal, btnPay, btnCancel
            });

            this.Controls.Add(controlPanel);
        }

        private void SetupEvents()
        {
            txtProductSearch.KeyDown += TxtProductSearch_KeyDown;
            txtCustomerSearch.KeyDown += TxtCustomerSearch_KeyDown;
            cbPriceLevel.SelectedIndexChanged += CbPriceLevel_SelectedIndexChanged;
            btnPay.Click += BtnPay_Click;
            btnCancel.Click += BtnCancel_Click;
            dgvSaleItems.MouseClick += DgvSaleItems_MouseClick;
        }

        private void StartClock()
        {
            clockTimer = new Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) => lblDateTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            clockTimer.Start();
            lblDateTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F5:
                    BtnPay_Click(sender, e);
                    break;
                case Keys.F9:
                    BtnCancel_Click(sender, e);
                    break;
            }
        }

        private async void TxtProductSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(txtProductSearch.Text))
            {
                await AddProductToSale(txtProductSearch.Text.Trim());
                txtProductSearch.Clear();
            }
        }

        private async void TxtCustomerSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(txtCustomerSearch.Text))
            {
                await SelectCustomer(txtCustomerSearch.Text.Trim());
            }
        }

        private void CbPriceLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            _currentPriceLevel = (PriceLevel)(cbPriceLevel.SelectedIndex + 1);
            UpdateSaleItemsPrices();
        }

        private async Task AddProductToSale(string searchText)
        {
            try
            {
                Product? product = null;

                // Try to find by code first
                product = await _productService.GetProductByCodeAsync(searchText);

                // If not found, search by name
                if (product == null)
                {
                    var products = await _productService.SearchProductsAsync(searchText);
                    if (products.Count == 1)
                    {
                        product = products[0];
                    }
                    else if (products.Count > 1)
                    {
                        // Show selection dialog if multiple products found
                        MessageBox.Show("พบสินค้าหลายรายการ กรุณาระบุรหัสสินค้าให้ชัดเจน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                if (product == null)
                {
                    MessageBox.Show("ไม่พบสินค้า", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Check if product already in sale
                var existingItem = _currentSaleItems.FirstOrDefault(x => x.ProductId == product.ProductId);
                if (existingItem != null)
                {
                    existingItem.Quantity++;
                    existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
                }
                else
                {
                    var saleItem = new SaleItem
                    {
                        ProductId = product.ProductId,
                        Product = product,
                        Quantity = 1,
                        UnitPrice = _productService.GetPriceByLevel(product, _currentPriceLevel),
                        TotalPrice = _productService.GetPriceByLevel(product, _currentPriceLevel)
                    };
                    _currentSaleItems.Add(saleItem);
                }

                UpdateDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SelectCustomer(string searchText)
        {
            try
            {
                Customer? customer = null;

                // Try to find by code first
                customer = await _customerService.GetCustomerByCodeAsync(searchText);

                // If not found, search by name
                if (customer == null)
                {
                    var customers = await _customerService.SearchCustomersAsync(searchText);
                    if (customers.Count == 1)
                    {
                        customer = customers[0];
                    }
                    else if (customers.Count > 1)
                    {
                        MessageBox.Show("พบลูกค้าหลายราย กรุณาระบุรหัสลูกค้าให้ชัดเจน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                if (customer != null)
                {
                    _selectedCustomer = customer;
                    txtCustomerSearch.Text = $"{customer.CustomerCode} - {customer.CustomerName}";
                    MessageBox.Show($"เลือกลูกค้า: {customer.CustomerName}", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("ไม่พบลูกค้า", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateSaleItemsPrices()
        {
            foreach (var item in _currentSaleItems)
            {
                if (item.Product != null)
                {
                    item.UnitPrice = _productService.GetPriceByLevel(item.Product, _currentPriceLevel);
                    item.TotalPrice = item.Quantity * item.UnitPrice;
                }
            }
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            // Update DataGridView
            dgvSaleItems.Rows.Clear();
            foreach (var item in _currentSaleItems)
            {
                dgvSaleItems.Rows.Add(
                    item.Product?.ProductCode,
                    item.Product?.ProductName,
                    item.Product?.Unit,
                    item.Quantity.ToString("0.000"),
                    item.UnitPrice.ToString("0.00"),
                    item.TotalPrice.ToString("0.00")
                );
            }

            // Update totals
            var subTotal = _currentSaleItems.Sum(x => x.TotalPrice);
            lblSubTotal.Text = $"รวมย่อย: {subTotal:N2} บาท";
            lblTotal.Text = $"รวมทั้งหมด: {subTotal:N2} บาท";
        }

        private void DgvSaleItems_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && dgvSaleItems.SelectedRows.Count > 0)
            {
                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("ลบสินค้า", null, RemoveSelectedItem);
                contextMenu.Items.Add("แก้ไขจำนวน", null, EditQuantity);
                contextMenu.Show(dgvSaleItems, e.Location);
            }
        }

        private void RemoveSelectedItem(object sender, EventArgs e)
        {
            if (dgvSaleItems.SelectedRows.Count > 0)
            {
                var selectedIndex = dgvSaleItems.SelectedRows[0].Index;
                if (selectedIndex < _currentSaleItems.Count)
                {
                    _currentSaleItems.RemoveAt(selectedIndex);
                    UpdateDisplay();
                }
            }
        }

        private void EditQuantity(object sender, EventArgs e)
        {
            if (dgvSaleItems.SelectedRows.Count > 0)
            {
                var selectedIndex = dgvSaleItems.SelectedRows[0].Index;
                if (selectedIndex < _currentSaleItems.Count)
                {
                    var item = _currentSaleItems[selectedIndex];
                    var result = Microsoft.VisualBasic.Interaction.InputBox(
                        "กรุณาระบุจำนวนใหม่:", "แก้ไขจำนวน", item.Quantity.ToString());

                    if (decimal.TryParse(result, out decimal newQuantity) && newQuantity > 0)
                    {
                        item.Quantity = newQuantity;
                        item.TotalPrice = item.Quantity * item.UnitPrice;
                        UpdateDisplay();
                    }
                }
            }
        }

        private async void BtnPay_Click(object sender, EventArgs e)
        {
            if (_currentSaleItems.Count == 0)
            {
                MessageBox.Show("ไม่มีสินค้าในบิล", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var paymentMethod = (PaymentMethod)(cbPaymentMethod.SelectedIndex + 1);
                var totalAmount = _currentSaleItems.Sum(x => x.TotalPrice);

                // Create sale
                var sale = await _saleService.CreateSaleAsync(_selectedCustomer?.CustomerId, _currentPriceLevel, paymentMethod, _currentSaleItems);

                // Complete sale (for now, assume full payment)
                await _saleService.CompleteSaleAsync(sale.SaleId, totalAmount);

                MessageBox.Show($"บันทึกการขายสำเร็จ\nเลขที่บิล: {sale.SaleNumber}", "สำเร็จ", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Reset for new sale
                NewSale();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (_currentSaleItems.Count > 0)
            {
                var result = MessageBox.Show("ต้องการยกเลิกบิลนี้หรือไม่?", "ยืนยัน", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    NewSale();
                }
            }
        }

        private void NewSale()
        {
            _currentSaleItems.Clear();
            _selectedCustomer = null;
            txtCustomerSearch.Clear();
            cbPriceLevel.SelectedIndex = 0;
            cbPaymentMethod.SelectedIndex = 0;
            UpdateDisplay();
        }

        private void Logout()
        {
            var result = MessageBox.Show("ต้องการออกจากระบบหรือไม่?", "ยืนยัน", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _authService.Logout();
                this.Hide();
                var loginForm = new LoginForm();
                loginForm.Show();
                loginForm.FormClosed += (s, args) => this.Close();
            }
        }

        private void OpenUserManagement()
        {
            MessageBox.Show("ฟีเจอร์จัดการผู้ใช้งานจะเพิ่มในอนาคต", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenProductManagement()
        {
            MessageBox.Show("ฟีเจอร์จัดการสินค้าจะเพิ่มในอนาคต", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenCustomerManagement()
        {
            MessageBox.Show("ฟีเจอร์จัดการสมาชิกจะเพิ่มในอนาคต", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenSalesReport()
        {
            MessageBox.Show("ฟีเจอร์รายงานยอดขายจะเพิ่มในอนาคต", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenStockReport()
        {
            MessageBox.Show("ฟีเจอร์รายงานสต็อกจะเพิ่มในอนาคต", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            clockTimer?.Stop();
            clockTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}