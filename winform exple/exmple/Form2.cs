using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SG71AuthClient;
using SG71AuthExample;

namespace exmple
{
    public partial class Form2 : Form
    {
        private readonly SG71Client _client;

        public Form2(SG71Client client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            InitializeComponent();
            LoadUserInfo();
        }

        private void LoadUserInfo()
        {
            var u = SG71Client.user_data.CurrentUser;
            if (u == null)
            {
                lblUser.Text = "—";
                lblExpires.Text = "—";
                lblHwid.Text = SG71Client.GetHWID();
                return;
            }

            lblUser.Text = u.Username ?? "—";
            lblExpires.Text = string.IsNullOrWhiteSpace(u.Expires) ? "—" : u.Expires;
            lblHwid.Text = string.IsNullOrWhiteSpace(u.HWID) ? SG71Client.GetHWID() : u.HWID;
            lblApp.Text = AuthConfig.AppName + " · v" + AuthConfig.AppVersion;
        }

        private async void btnCheckUpdate_Click(object sender, EventArgs e)
        {
            btnCheckUpdate.Enabled = false;
            lblMessage.Text = "Checking for updates…";
            lblMessage.ForeColor = Color.DimGray;
            try
            {
                var check = await _client.CheckForUpdateAsync();
                if (check.UpdateRequired)
                {
                    lblMessage.Text = check.GetDisplayMessage();
                    if (MessageBox.Show(
                            check.GetDisplayMessage() + "\n\nDownload update now?",
                            "Update",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        var (ok, path) = await SelfUpdater.DownloadUpdateBesideExeAsync(check.UpdateUrl);
                        if (ok)
                            SelfUpdater.ApplyUpdateAndRestart(path);
                        else
                            lblMessage.Text = "Download failed.";
                    }
                }
                else if (!check.IsOk)
                {
                    lblMessage.Text = check.GetDisplayMessage();
                    lblMessage.ForeColor = Color.IndianRed;
                }
                else
                {
                    lblMessage.Text = check.GetDisplayMessage();
                    lblMessage.ForeColor = Color.DimGray;
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = ex.Message;
                lblMessage.ForeColor = Color.IndianRed;
            }
            finally
            {
                btnCheckUpdate.Enabled = true;
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SG71Client.user_data.CurrentUser = null;
            Close();
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }
    }
}
