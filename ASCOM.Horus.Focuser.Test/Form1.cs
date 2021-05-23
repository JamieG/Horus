using System;
using System.Windows.Forms;

namespace ASCOM.Horus
{
    public partial class Form1 : Form
    {

        private ASCOM.DriverAccess.Focuser _driver;

        public Form1()
        {
            InitializeComponent();
            SetUIState();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsConnected)
                _driver.Connected = false;

            Properties.Settings.Default.Save();
        }

        private void buttonChoose_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.DriverId = ASCOM.DriverAccess.Focuser.Choose(Properties.Settings.Default.DriverId);
            SetUIState();
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (IsConnected)
            {
                _driver.Connected = false;
            }
            else
            {
                _driver = new ASCOM.DriverAccess.Focuser(Properties.Settings.Default.DriverId);
                _driver.Connected = true;
            }
            SetUIState();
        }

        private void SetUIState()
        {
            buttonConnect.Enabled = !string.IsNullOrEmpty(Properties.Settings.Default.DriverId);
            buttonChoose.Enabled = !IsConnected;
            buttonConnect.Text = IsConnected ? "Disconnect" : "Connect";
        }

        private bool IsConnected
        {
            get
            {
                return ((this._driver != null) && (_driver.Connected == true));
            }
        }

        private void buttonMoveSet_Click(object sender, EventArgs e)
        {
            if (_driver?.Connected ?? false)
            {
                _driver.Move((int) numericUpDownMove.Value);
            }
        }
    }
}
