using System;
using System.Diagnostics;
using System.Windows.Forms;
using Clover.RemotePay;
using com.clover.remotepay.sdk;
using com.clover.remotepay.transport;

namespace SnpdConnectionExample
{
    public partial class Form1 : Form
    {
       
        string CurrentPairingToken = ""; // If you store & load this value, you can skip pairing codes on the device most of the time.
        CloverEventConnector clover; // Event connector is just a simple wrapper around the CloverConnector & CloverConnectorListener to get listener messages as C# events, class code can be viewed in the SDK github repo
        ICloverConnector connector;
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// When the user hits the connect button, create a Clover network connection: WebSocket transport to Secure Network Pay Display (SNPD) running on the device
        /// Note: needs the wss network address supplied in the DeviceAddressTextBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectButton_Click(object sender, EventArgs e)
        {
            ClearOutput();
            Output("Initiating Connection...");

            try
            {
                connector = CloverConnectorFactory.CreateWebSocketConnector(
                    Endpoint, 
                    RAID, 
                    PosName, 
                    SerialNumber,
                    null,
                    PairingCodeHandler,
                    PairingSuccessHandler,
                    PairingStateHandler);

                if (connector == null)
                {
                    Output("Failed to create WebSocket connector.");
                }
            }
            catch (Exception ex)
            {
                Output($"Exception while creating WebSocket connector: {ex.Message}");
                Debug.WriteLine(ex.ToString());
            }

            clover = new CloverEventConnector(connector);

            clover.Message += Clover_Message;
            clover.DeviceError += Clover_DeviceError;
            clover.DeviceConnected += Clover_DeviceConnected;
            clover.DeviceReady += Clover_DeviceReady;
            clover.DeviceDisconnected += Clover_DeviceDisconnected;

            clover.InitializeConnection();
            //clover.ShowThankYouScreen();
        }

        private void MsgButton_Click(object sender, EventArgs e)
        {
            clover.ShowThankYouScreen();
        }
        //
        // Move background thread calls to UI thread for UI updates
        //

        /// <summary>
        /// Output to the StatusTextBox (threadsafe)
        /// </summary>
        void Output(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action<string>)Output, message);
            }
            else
            {
                StatusTextBox.Text += message + "\r\n";
                Debug.WriteLine(message);
            }
        }

        /// <summary>
        /// Clear the StatusTextBox output of old messages (threadsafe)
        /// </summary>
        void ClearOutput()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)ClearOutput);
            }
            else
            {
                StatusTextBox.Text = "";
            }
        }

        //
        // Clover WebSocketConnector Handlers
        // 

        void PairingCodeHandler(string code)
        {
            // Pairing requires a code entered on the device
            //string fixedPairingCode = "1111";
            Output($"Pairing code {code}");
        }

        void PairingSuccessHandler(string token)
        {
            // Every successful connection results in a new token
            CurrentPairingToken = token;
            Output($"Paired successfully, silent pair token for next time: {token}");
        }

        void PairingStateHandler(string state, string token)
        {
            // This won't happen in current 4.0.2 and earlier apis, future device plans
            Output($"Pairing State Changed: {state}, {token}");
        }

        //
        // Clover Event Connector Handlers
        //

        private void Clover_Message(object sender, CloverEventArgs e)
        {
            // if we didn't already mark the message handled in one of the below handlers, report it
            if (!e.Handled)
            {
                Debug.WriteLine("----Clover_Message---");
                Debug.WriteLine(e.ToString());
                Debug.WriteLine("----Clover_Message---");
            }
        }

        /// <summary>
        /// SDK detected device disconnected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clover_DeviceDisconnected(object sender, DeviceDisconnectedEventArgs e)
        {
            e.Handled = true;
            Debug.WriteLine("----Clover_DeviceDisconnected---");
            Output($"Device Disconnected");
            Debug.WriteLine("----Clover_DeviceDisconnected---");
        }

        /// <summary>
        /// Device is connected and ready to begin processing
        /// </summary>
        private void Clover_DeviceReady(object sender, DeviceReadyEventArgs e)
        {
            e.Handled = true;
            Debug.WriteLine("----Clover_DeviceReady---");
            Debug.WriteLine(e.ToString());
            Debug.WriteLine("----Clover_DeviceReady---");
        }

        /// <summary>
        /// Device is connected but not ready to begin processing, wait for the DeviceReady message
        /// </summary>
        private void Clover_DeviceConnected(object sender, DeviceConnectedEventArgs e)
        {
            e.Handled = true;
            Debug.WriteLine("----Clover_DeviceConnected---");
            Output($"Device Connected");
            Debug.WriteLine("----Clover_DeviceConnected---");
        }

        /// <summary>
        /// SDK reports an error from SDK or device layer
        /// </summary>
        private void Clover_DeviceError(object sender, DeviceErrorEventArgs e)
        {
            e.Handled = true;
            Debug.WriteLine("----Clover_DeviceError---");
            Output($"Device Error: {e}");
            Debug.WriteLine("----Clover_DeviceError---");
        }
    }
}
