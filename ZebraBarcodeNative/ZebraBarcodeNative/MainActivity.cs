using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.Barcode;
using System.Collections.Generic;
using System;
using Android.Util;
using Android.Views;
using Android.Content;
using System.Threading;

namespace ZebraBarcodeNative
{
    [Activity(Label = "BarcodeSample1", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Activity, EMDKManager.IEMDKListener
    {
        private EMDKManager emdkManager = null;
        private BarcodeManager barcodeManager = null;
        private Scanner scanner = null;

        // Declare a flag for continuous scan mode
        private bool isContinuousMode = true;

        private IList<ScannerInfo> scannerList = null;

        private TextView textViewData = null;
        private TextView textViewStatus = null;
        private int dataCount = 0;
        private string statusString = "";
        Button buttonStartScan;
        Button buttonStopScan;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.activity_main);

            textViewData = FindViewById<TextView>(Resource.Id.textViewData) as TextView;
            textViewStatus = FindViewById<TextView>(Resource.Id.textViewStatus) as TextView;

            buttonStartScan = FindViewById<Button>(Resource.Id.buttonStartScan);
            buttonStartScan.Click += buttonStartScan_Click;

            buttonStopScan = FindViewById<Button>(Resource.Id.buttonStopScan);
            buttonStopScan.Click += buttonStopScan_Click;

            // The EMDKManager object will be created and returned in the callback
            EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);

            // Check the return status of GetEMDKManager
            if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
            {
                // EMDKManager object initialization failed
                textViewStatus.Text = "Status: EMDKManager object creation failed.";
            }
            else
            {
                // EMDKManager object initialization succeeded
                textViewStatus.Text = "Status: EMDKManager object creation succeeded.";
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // De-initialize scanner
            DeInitScanner();

            // Clean up the objects created by EMDK manager
            if (barcodeManager != null)
            {
                // Remove connection listener
                barcodeManager.Connection -= barcodeManager_Connection;
                barcodeManager = null;
            }

            if (emdkManager != null)
            {
                emdkManager.Release();
                emdkManager = null;
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            // The application is in background
            // De-initialize scanner
            DeInitScanner();

            if (barcodeManager != null)
            {
                // Remove connection listener
                barcodeManager.Connection -= barcodeManager_Connection;
                barcodeManager = null;

                // Clear scanner list
                scannerList = null;
            }

            // Release the barcode manager resources
            if (emdkManager != null)
            {
                emdkManager.Release(EMDKManager.FEATURE_TYPE.Barcode);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            // The application is in foreground 
            
            // Acquire the barcode manager resources
            if (emdkManager != null)
            {
                try
                {
                    barcodeManager = (BarcodeManager)emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Barcode);

                    if (barcodeManager != null)
                    {
                        // Add connection listener
                        barcodeManager.Connection += barcodeManager_Connection;
                    }
                }
                catch (Exception e)
                {
                    textViewStatus.Text = "Status: BarcodeManager object creation failed.";
                    Console.WriteLine("Exception: " + e.StackTrace);
                }
            }
        }

        #region IEMDKListener Members

        public void OnClosed()
        {
            // This callback will be issued when the EMDK closes unexpectedly.

            if (emdkManager != null)
            {
                if (barcodeManager != null)
                {
                    // Remove connection listener
                    barcodeManager.Connection -= barcodeManager_Connection;
                    barcodeManager = null;
                }

                // Release all the resources
                emdkManager.Release();
                emdkManager = null;
            }

            textViewStatus.Text = "Status: EMDK closed unexpectedly! Please close and restart the application.";
        }

        public void OnOpened(EMDKManager emdkManagerInstance)
        {
            // This callback will be issued when the EMDK is ready to use.
            textViewStatus.Text = "Status: EMDK open success.";

            this.emdkManager = emdkManagerInstance;

            try
            {
                // Acquire the barcode manager resources
                barcodeManager = (BarcodeManager)emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Barcode);

                if (barcodeManager != null)
                {
                    // Add connection listener
                    barcodeManager.Connection += barcodeManager_Connection;
                }
            }
            catch (Exception e)
            {
                textViewStatus.Text = "Status: BarcodeManager object creation failed.";
                Console.WriteLine("Exception:" + e.StackTrace);
            }
        }

        void barcodeManager_Connection(object sender, BarcodeManager.ScannerConnectionEventArgs e)
        {
            string status;
            string scannerName = "";

            ScannerInfo scannerInfo = e.P0;
            BarcodeManager.ConnectionState connectionState = e.P1;

            string statusBT = connectionState.ToString();
            string scannerNameBT = scannerInfo.FriendlyName;

            if (scannerList.Count != 0)
            {
                scannerName = scannerList[0].FriendlyName;
            }

            if (scannerName.ToLower().Equals(scannerNameBT.ToLower()))
            {
                status = "Status: " + scannerNameBT + ":" + statusBT;
                RunOnUiThread(() => textViewStatus.Text = status);

                if (connectionState == BarcodeManager.ConnectionState.Connected)
                {
                    // Bluetooth scanner connected
                    // De-initialize scanner
                    DeInitScanner();
                    // Initialize scanner
                    InitScanner();
                    scanner.TriggerType = Scanner.TriggerTypes.SoftAlways;// TO MUSI ZOSTAC

                    ScannerConfig config = scanner.GetConfig(); // SCANNER CONFIG
                    config.DecoderParams.Ean8.Enabled = true;
                    config.DecoderParams.Ean13.Enabled = true;
                    scanner.SetConfig(config);
                }

                if (connectionState == BarcodeManager.ConnectionState.Disconnected)
                {
                    // Reset continuous flag 
                    isContinuousMode = false;

                    // De-initialize scanner
                    DeInitScanner();
                }
                status = "Status: " + scannerNameBT + ":" + statusBT;
                RunOnUiThread(() => textViewStatus.Text = status);
            }
            else
            {
                status = "Status: " + statusString + " " + scannerNameBT + ":" + statusBT;
                RunOnUiThread(() => textViewStatus.Text = status);
            }
        }

        #endregion

        void buttonStartScan_Click(object sender, EventArgs e)
        {
            if (scanner == null)
            {
                InitScanner();
            }

            if (scanner != null)
            {
                try
                {
                    if (scanner.IsEnabled)
                    {
                        // Set continuous flag
                        isContinuousMode = true;

                        // Submit a new read.
                        scanner.Read();
                    }
                    else
                    {
                        textViewStatus.Text = "Status: Scanner is not enabled";
                    }
                }
                catch (ScannerException ex)
                {
                    textViewStatus.Text = "Status: " + ex.Message;
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        void buttonStopScan_Click(object sender, EventArgs e)
        {
            if (scanner != null)
            {
                try
                {
                    // Reset continuous flag 
                    isContinuousMode = false;

                    // Cancel the pending read.
                    scanner.CancelRead();
                }
                catch (ScannerException ex)
                {
                    textViewStatus.Text = "Status: " + ex.Message;
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
        
        private void InitScanner()
        {
            if (scanner == null)
            {
                scanner = barcodeManager.GetDevice(barcodeManager.SupportedDevicesInfo[0]);

                if (scanner != null)
                {
                    // Add data listener
                    scanner.Data += scanner_Data;

                    // Add status listener
                    scanner.Status += scanner_Status;

                    try
                    {
                        // Enable the scanner
                        scanner.Enable();
                    }
                    catch (ScannerException e)
                    {
                        textViewStatus.Text = "Status: " + e.Message;
                        Console.WriteLine(e.StackTrace);
                    }
                }
                else
                {
                    textViewStatus.Text = "Status: Failed to initialize the scanner device.";
                }
            }
        }

        void scanner_Status(object sender, Scanner.StatusEventArgs e)
        {
            StatusData statusData = e.P0;
            StatusData.ScannerStates state = e.P0.State;

            if (state == StatusData.ScannerStates.Idle)
            {
                statusString = "Status: " + statusData.FriendlyName + " is enabled and idle...";
                RunOnUiThread(() => textViewStatus.Text = statusString);

                if (isContinuousMode)//jezeli brakuje isContinuousMode to skanner działa zawsze
                {
                    try
                    {
                        // An attempt to use the scanner continuously and rapidly (with a delay < 100 ms between scans) 
                        // may cause the scanner to pause momentarily before resuming the scanning. 
                        // Hence add some delay (>= 100ms) before submitting the next read.
                        try
                        {
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }

                        // Submit another read to keep the continuation
                        scanner.Read();
                    }
                    catch (ScannerException ex)
                    {
                        statusString = "Status: " + ex.Message;
                        RunOnUiThread(() => textViewStatus.Text = statusString);
                        Console.WriteLine(ex.StackTrace);
                    }
                    catch (NullReferenceException ex)
                    {
                        statusString = "Status: An error has occurred.";
                        RunOnUiThread(() => textViewStatus.Text = statusString);
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }

            if (state == StatusData.ScannerStates.Waiting)
            {
                statusString = "Status: Scanner is waiting for trigger press...";
                RunOnUiThread(() =>
                {
                    textViewStatus.Text = statusString;
                });
            }

            if (state == StatusData.ScannerStates.Scanning)
            {
                statusString = "Status: Scanning...";
                RunOnUiThread(() =>
                {
                    textViewStatus.Text = statusString;
                });
            }

            if (state == StatusData.ScannerStates.Disabled)
            {
                statusString = "Status: " + statusData.FriendlyName + " is disabled.";
                RunOnUiThread(() =>
                {
                    textViewStatus.Text = statusString;
                });
            }

            if (state == StatusData.ScannerStates.Error)
            {
                statusString = "Status: An error has occurred.";
                RunOnUiThread(() =>
                {
                    textViewStatus.Text = statusString;
                });
            }
        }

        void scanner_Data(object sender, Scanner.DataEventArgs e)
        {
            ScanDataCollection scanDataCollection = e.P0;

            if ((scanDataCollection != null) && (scanDataCollection.Result == ScannerResults.Success))
            {
                IList<ScanDataCollection.ScanData> scanData = scanDataCollection.GetScanData();

                foreach (ScanDataCollection.ScanData data in scanData)
                {
                    string dataString = data.Data;
                    RunOnUiThread(() => DisplayScanData(dataString));
                }
            }
        }

        private void DeInitScanner()
        {
            if (scanner != null)
            {
                try
                {
                    // Cancel if there is any pending read
                    scanner.CancelRead();

                    // Disable the scanner 
                    scanner.Disable();
                }
                catch (ScannerException e)
                {
                    textViewStatus.Text = "Status: " + e.Message;
                    Console.WriteLine(e.StackTrace);
                }

                // Remove data listener
                scanner.Data -= scanner_Data;

                // Remove status listener
                scanner.Status -= scanner_Status;

                try
                {
                    // Release the scanner
                    scanner.Release();
                }
                catch (ScannerException e)
                {
                    textViewStatus.Text = "Status: " + e.Message;
                    Console.WriteLine(e.StackTrace);
                }

                scanner = null;
            }
        }

        private void DisplayScanData(string data)
        {
            if (dataCount++ > 100)
            {
                // Clear the cache after 100 scans
                textViewData.Text = "";
                dataCount = 0;
            }

            textViewData.Append(data + "\r\n");

            var scrollview = FindViewById<ScrollView>(Resource.Id.scrollView1);
            scrollview.FullScroll(FocusSearchDirection.Down);
        }
    }
}