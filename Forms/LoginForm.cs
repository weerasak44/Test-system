using POS_System.Services;

namespace POS_System.Forms
{
    public partial class LoginForm : Form
    {
        private readonly AuthService _authService;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnExit;
        private Label lblStoreName;
        private Panel pnlLogin;

        public LoginForm()
        {
            _authService = new AuthService();
            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += LoginForm_KeyDown;
        }

        private void InitializeComponent()
        {
            this.Size = new Size(400, 300);
            this.Text = "ระบบ POS - เข้าสู่ระบบ";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Store Name Label
            lblStoreName = new Label
            {
                Text = "ระบบ POS",
                Font = new Font("Tahoma", 18, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(50, 30),
                Size = new Size(300, 40)
            };

            // Login Panel
            pnlLogin = new Panel
            {
                Location = new Point(50, 90),
                Size = new Size(300, 150),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Username
            var lblUsername = new Label
            {
                Text = "ชื่อผู้ใช้:",
                Location = new Point(20, 20),
                Size = new Size(80, 23),
                Font = new Font("Tahoma", 9)
            };

            txtUsername = new TextBox
            {
                Location = new Point(100, 20),
                Size = new Size(180, 23),
                Font = new Font("Tahoma", 9)
            };

            // Password
            var lblPassword = new Label
            {
                Text = "รหัสผ่าน:",
                Location = new Point(20, 50),
                Size = new Size(80, 23),
                Font = new Font("Tahoma", 9)
            };

            txtPassword = new TextBox
            {
                Location = new Point(100, 50),
                Size = new Size(180, 23),
                UseSystemPasswordChar = true,
                Font = new Font("Tahoma", 9)
            };

            // Login Button
            btnLogin = new Button
            {
                Text = "เข้าสู่ระบบ",
                Location = new Point(100, 90),
                Size = new Size(80, 30),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Font = new Font("Tahoma", 9)
            };
            btnLogin.Click += BtnLogin_Click;

            // Exit Button
            btnExit = new Button
            {
                Text = "ออก",
                Location = new Point(200, 90),
                Size = new Size(80, 30),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font("Tahoma", 9)
            };
            btnExit.Click += BtnExit_Click;

            // Add controls
            pnlLogin.Controls.AddRange(new Control[] {
                lblUsername, txtUsername, lblPassword, txtPassword, btnLogin, btnExit
            });

            this.Controls.AddRange(new Control[] {
                lblStoreName, pnlLogin
            });

            // Set default values for testing
            txtUsername.Text = "admin";
            txtPassword.Text = "admin123";
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("กรุณากรอกชื่อผู้ใช้และรหัสผ่าน", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "กำลังตรวจสอบ...";

            try
            {
                bool success = await _authService.LoginAsync(txtUsername.Text.Trim(), txtPassword.Text);

                if (success)
                {
                    this.Hide();
                    var mainForm = new MainForm();
                    mainForm.Show();
                    mainForm.FormClosed += (s, args) => this.Close();
                }
                else
                {
                    MessageBox.Show("ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Clear();
                    txtUsername.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"เกิดข้อผิดพลาด: {ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "เข้าสู่ระบบ";
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}