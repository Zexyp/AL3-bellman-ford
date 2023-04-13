using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VeryFunnyGraphs.Forms
{
    public partial class PreferencesForm : Form
    {
        private bool save = false;

        public PreferencesForm()
        {
            InitializeComponent();
        }

        private void testButton_Click(object sender, EventArgs e)
        {
            try
            {
                Connector connector = new Connector();
                // invalid json payload to test the connection
                MessageBox.Show(connector.Use(addressTextBox.Text, (int)portNumericUpDown.Value, "{\"test\":\"pp\"}"));
            }
            catch (Exception)
            {
                MessageBox.Show("What have you done?!");
                return;
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            save = true;

            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            save = false;

            this.Close();
        }

        public bool Edit(Preferences preferences, out Preferences result)
        {
            addressTextBox.Text = preferences.host;
            portNumericUpDown.Value = preferences.port;

            ShowDialog();
            
            result = new Preferences();

            result.host = addressTextBox.Text;
            result.port = (int)portNumericUpDown.Value;

            return save;
        }
    }
}
