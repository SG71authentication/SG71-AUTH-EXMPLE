namespace exmple
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblApi = new System.Windows.Forms.Label();
            this.lblUser = new System.Windows.Forms.Label();
            this.lblPass = new System.Windows.Forms.Label();
            this.lblKey = new System.Windows.Forms.Label();
            this.user = new System.Windows.Forms.TextBox();
            this.pass = new System.Windows.Forms.TextBox();
            this.key = new System.Windows.Forms.TextBox();
            this.login = new System.Windows.Forms.Button();
            this.register = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panelMain = new System.Windows.Forms.Panel();
            this.panelMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(64, 120, 220);
            this.lblTitle.Location = new System.Drawing.Point(24, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(150, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "SG71 Auth Login";
            // 
            // lblApi
            // 
            this.lblApi.AutoSize = true;
            this.lblApi.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblApi.ForeColor = System.Drawing.Color.Gray;
            this.lblApi.Location = new System.Drawing.Point(26, 48);
            this.lblApi.Name = "lblApi";
            this.lblApi.Size = new System.Drawing.Size(28, 13);
            this.lblApi.TabIndex = 1;
            this.lblApi.Text = "API:";
            // 
            // panelMain
            // 
            this.panelMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelMain.BackColor = System.Drawing.Color.FromArgb(28, 28, 36);
            this.panelMain.Controls.Add(this.lblUser);
            this.panelMain.Controls.Add(this.lblPass);
            this.panelMain.Controls.Add(this.lblKey);
            this.panelMain.Controls.Add(this.user);
            this.panelMain.Controls.Add(this.pass);
            this.panelMain.Controls.Add(this.key);
            this.panelMain.Controls.Add(this.login);
            this.panelMain.Controls.Add(this.register);
            this.panelMain.Location = new System.Drawing.Point(24, 72);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(352, 220);
            this.panelMain.TabIndex = 2;
            // 
            // lblUser
            // 
            this.lblUser.AutoSize = true;
            this.lblUser.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lblUser.Location = new System.Drawing.Point(20, 24);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(58, 13);
            this.lblUser.TabIndex = 0;
            this.lblUser.Text = "Username";
            // 
            // user
            // 
            this.user.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.user.Location = new System.Drawing.Point(20, 42);
            this.user.Name = "user";
            this.user.Size = new System.Drawing.Size(312, 20);
            this.user.TabIndex = 1;
            // 
            // lblPass
            // 
            this.lblPass.AutoSize = true;
            this.lblPass.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lblPass.Location = new System.Drawing.Point(20, 72);
            this.lblPass.Name = "lblPass";
            this.lblPass.Size = new System.Drawing.Size(53, 13);
            this.lblPass.TabIndex = 2;
            this.lblPass.Text = "Password";
            // 
            // pass
            // 
            this.pass.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pass.Location = new System.Drawing.Point(20, 90);
            this.pass.Name = "pass";
            this.pass.Size = new System.Drawing.Size(312, 20);
            this.pass.TabIndex = 3;
            this.pass.UseSystemPasswordChar = true;
            // 
            // lblKey
            // 
            this.lblKey.AutoSize = true;
            this.lblKey.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lblKey.Location = new System.Drawing.Point(20, 120);
            this.lblKey.Name = "lblKey";
            this.lblKey.Size = new System.Drawing.Size(68, 13);
            this.lblKey.TabIndex = 4;
            this.lblKey.Text = "License key";
            // 
            // key
            // 
            this.key.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.key.Location = new System.Drawing.Point(20, 138);
            this.key.Name = "key";
            this.key.Size = new System.Drawing.Size(312, 20);
            this.key.TabIndex = 5;
            // 
            // login
            // 
            this.login.BackColor = System.Drawing.Color.FromArgb(64, 120, 220);
            this.login.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.login.ForeColor = System.Drawing.Color.White;
            this.login.Location = new System.Drawing.Point(20, 176);
            this.login.Name = "login";
            this.login.Size = new System.Drawing.Size(148, 32);
            this.login.TabIndex = 6;
            this.login.Text = "Login";
            this.login.UseVisualStyleBackColor = false;
            this.login.Click += new System.EventHandler(this.login_Click);
            // 
            // register
            // 
            this.register.BackColor = System.Drawing.Color.FromArgb(50, 50, 60);
            this.register.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.register.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.register.Location = new System.Drawing.Point(184, 176);
            this.register.Name = "register";
            this.register.Size = new System.Drawing.Size(148, 32);
            this.register.TabIndex = 7;
            this.register.Text = "Register";
            this.register.UseVisualStyleBackColor = false;
            this.register.Click += new System.EventHandler(this.register_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblStatus.Location = new System.Drawing.Point(24, 308);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(352, 32);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "Ready";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(18, 18, 24);
            this.ClientSize = new System.Drawing.Size(400, 360);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.lblApi);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SG71 Auth";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblApi;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.Label lblPass;
        private System.Windows.Forms.Label lblKey;
        private System.Windows.Forms.TextBox user;
        private System.Windows.Forms.TextBox pass;
        private System.Windows.Forms.TextBox key;
        private System.Windows.Forms.Button login;
        private System.Windows.Forms.Button register;
        private System.Windows.Forms.Label lblStatus;
    }
}
