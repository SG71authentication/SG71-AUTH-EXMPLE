namespace exmple
{
    partial class Form2
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
            this.lblApp = new System.Windows.Forms.Label();
            this.lblUserCaption = new System.Windows.Forms.Label();
            this.lblUser = new System.Windows.Forms.Label();
            this.lblExpiresCaption = new System.Windows.Forms.Label();
            this.lblExpires = new System.Windows.Forms.Label();
            this.lblHwidCaption = new System.Windows.Forms.Label();
            this.lblHwid = new System.Windows.Forms.Label();
            this.btnCheckUpdate = new System.Windows.Forms.Button();
            this.btnLogout = new System.Windows.Forms.Button();
            this.lblMessage = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(120)))), ((int)(((byte)(220)))));
            this.lblTitle.Location = new System.Drawing.Point(24, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(109, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Dashboard";
            // 
            // lblApp
            // 
            this.lblApp.AutoSize = true;
            this.lblApp.ForeColor = System.Drawing.Color.Gray;
            this.lblApp.Location = new System.Drawing.Point(26, 50);
            this.lblApp.Name = "lblApp";
            this.lblApp.Size = new System.Drawing.Size(26, 13);
            this.lblApp.TabIndex = 1;
            this.lblApp.Text = "App";
            // 
            // lblUserCaption
            // 
            this.lblUserCaption.AutoSize = true;
            this.lblUserCaption.ForeColor = System.Drawing.Color.DimGray;
            this.lblUserCaption.Location = new System.Drawing.Point(26, 88);
            this.lblUserCaption.Name = "lblUserCaption";
            this.lblUserCaption.Size = new System.Drawing.Size(55, 13);
            this.lblUserCaption.TabIndex = 2;
            this.lblUserCaption.Text = "Username";
            // 
            // lblUser
            // 
            this.lblUser.AutoSize = true;
            this.lblUser.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblUser.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lblUser.Location = new System.Drawing.Point(26, 104);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(23, 19);
            this.lblUser.TabIndex = 3;
            this.lblUser.Text = "—";
            // 
            // lblExpiresCaption
            // 
            this.lblExpiresCaption.AutoSize = true;
            this.lblExpiresCaption.ForeColor = System.Drawing.Color.DimGray;
            this.lblExpiresCaption.Location = new System.Drawing.Point(26, 136);
            this.lblExpiresCaption.Name = "lblExpiresCaption";
            this.lblExpiresCaption.Size = new System.Drawing.Size(41, 13);
            this.lblExpiresCaption.TabIndex = 4;
            this.lblExpiresCaption.Text = "Expires";
            // 
            // lblExpires
            // 
            this.lblExpires.AutoSize = true;
            this.lblExpires.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lblExpires.Location = new System.Drawing.Point(26, 152);
            this.lblExpires.Name = "lblExpires";
            this.lblExpires.Size = new System.Drawing.Size(13, 13);
            this.lblExpires.TabIndex = 5;
            this.lblExpires.Text = "—";
            // 
            // lblHwidCaption
            // 
            this.lblHwidCaption.AutoSize = true;
            this.lblHwidCaption.ForeColor = System.Drawing.Color.DimGray;
            this.lblHwidCaption.Location = new System.Drawing.Point(26, 180);
            this.lblHwidCaption.Name = "lblHwidCaption";
            this.lblHwidCaption.Size = new System.Drawing.Size(37, 13);
            this.lblHwidCaption.TabIndex = 6;
            this.lblHwidCaption.Text = "HWID";
            // 
            // lblHwid
            // 
            this.lblHwid.Font = new System.Drawing.Font("Consolas", 9F);
            this.lblHwid.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lblHwid.Location = new System.Drawing.Point(26, 196);
            this.lblHwid.Name = "lblHwid";
            this.lblHwid.Size = new System.Drawing.Size(348, 40);
            this.lblHwid.TabIndex = 7;
            this.lblHwid.Text = "—";
            // 
            // btnCheckUpdate
            // 
            this.btnCheckUpdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(60)))));
            this.btnCheckUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCheckUpdate.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.btnCheckUpdate.Location = new System.Drawing.Point(29, 252);
            this.btnCheckUpdate.Name = "btnCheckUpdate";
            this.btnCheckUpdate.Size = new System.Drawing.Size(160, 32);
            this.btnCheckUpdate.TabIndex = 8;
            this.btnCheckUpdate.Text = "Check for updates";
            this.btnCheckUpdate.UseVisualStyleBackColor = false;
            this.btnCheckUpdate.Click += new System.EventHandler(this.btnCheckUpdate_Click);
            // 
            // btnLogout
            // 
            this.btnLogout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.ForeColor = System.Drawing.Color.White;
            this.btnLogout.Location = new System.Drawing.Point(214, 252);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(160, 32);
            this.btnLogout.TabIndex = 9;
            this.btnLogout.Text = "Logout";
            this.btnLogout.UseVisualStyleBackColor = false;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            // 
            // lblMessage
            // 
            this.lblMessage.ForeColor = System.Drawing.Color.DimGray;
            this.lblMessage.Location = new System.Drawing.Point(26, 300);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(348, 40);
            this.lblMessage.TabIndex = 10;
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(24)))));
            this.ClientSize = new System.Drawing.Size(400, 360);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.btnLogout);
            this.Controls.Add(this.btnCheckUpdate);
            this.Controls.Add(this.lblHwid);
            this.Controls.Add(this.lblHwidCaption);
            this.Controls.Add(this.lblExpires);
            this.Controls.Add(this.lblExpiresCaption);
            this.Controls.Add(this.lblUser);
            this.Controls.Add(this.lblUserCaption);
            this.Controls.Add(this.lblApp);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form2";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SG71 Auth — Dashboard";
            this.Load += new System.EventHandler(this.Form2_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblApp;
        private System.Windows.Forms.Label lblUserCaption;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.Label lblExpiresCaption;
        private System.Windows.Forms.Label lblExpires;
        private System.Windows.Forms.Label lblHwidCaption;
        private System.Windows.Forms.Label lblHwid;
        private System.Windows.Forms.Button btnCheckUpdate;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.Label lblMessage;
    }
}
