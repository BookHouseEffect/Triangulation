#pragma warning disable 0618

using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Telephony;
using Android.Telephony.Gsm;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Net;
using System.IO;
using Android.Net;
using Android.Locations;
using System.Linq;

namespace Triangulation
{
    public class Data
    {

        public static List<CellRecords> listOfRecords;
        public static List<AnalyzeItem> AnalyzedTableData;
        public static string Unknown;
        public static string noData;

        public Data()
        {
            listOfRecords = new List<CellRecords>();
            AnalyzedTableData = new List<AnalyzeItem>();
            Unknown = string.Empty;
            noData = string.Empty;
        }

        public static void AnalyzeData()
        {
            AnalyzedTableData = new List<AnalyzeItem>();
            if (listOfRecords.Count == 0)
                return;

            for (int i = 0; i < listOfRecords.Count; i++)
            {
                CellRecords cr = listOfRecords[i];
                if (cr.Cells.Count == 0)
                    break;

                for (int j = 0; j < cr.Cells.Count; j++)
                {
                    int index = -1;

                    if (AnalyzedTableData.Count != 0)
                    {
                        for (int k = 0; k < AnalyzedTableData.Count; k++)
                        {
                            AnalyzeItem ai = AnalyzedTableData[k];
                            if (ai.MCC == cr.MCC && ai.MNC == cr.MNC && ai.LAC == cr.Cells[j].LAC && ai.CellID == cr.Cells[j].CellID)
                            {
                                index = k;
                                ai.RSSIs[i] = cr.Cells[j].RSSI;
                                break;
                            }
                        }
                    }

                    if (AnalyzedTableData.Count == 0 || index == -1)
                    {
                        AnalyzeItem ai = new AnalyzeItem();
                        ai.MCC = cr.MCC;
                        ai.MNC = cr.MNC;
                        ai.LAC = cr.Cells[j].LAC;
                        ai.CellID = cr.Cells[j].CellID;
                        ai.RSSIs = new List<int>();
                        for (int l = 0; l < listOfRecords.Count; l++)
                            ai.RSSIs.Add(0);
                        ai.RSSIs[i] = cr.Cells[j].RSSI;
                        AnalyzedTableData.Add(ai);
                    }
                }
            }

            for (int i = 0; i < AnalyzedTableData.Count; i++)
            {
                AnalyzedTableData[i].findMinAvgMax();
            }
        }

        public static void TryFetchingCellCoordinates()
        {
            if (AnalyzedTableData != null)
            {
                for (int i = 0; i < AnalyzedTableData.Count; i++)
                {
                    AnalyzedTableData[i].fetchCoordinates();
                }
            }

            double minSum = 0, avgSum = 0, maxSum = 0;
            for (int i = 0; i < AnalyzedTableData.Count; i++)
            {
                AnalyzeItem a = AnalyzedTableData[i];
                a.findMinAvgMax();
                minSum += (-113 <= a.min && a.min <= -51 && a.lat != string.Empty && a.lon != string.Empty) ? AnalyzedTableData[i].min + 114 : 0;
                avgSum += (-113 <= a.avg && a.avg <= -51 && a.lat != string.Empty && a.lon != string.Empty) ? AnalyzedTableData[i].avg + 114 : 0;
                maxSum += (-113 <= a.max && a.max <= -51 && a.lat != string.Empty && a.lon != string.Empty) ? AnalyzedTableData[i].max + 114 : 0;
            }

            for (int i = 0; i < AnalyzedTableData.Count; i++)
            {
                AnalyzeItem a = AnalyzedTableData[i];
                a.minDist = (-113 <= a.min && a.min <= -51 && a.lat != string.Empty && a.lon != string.Empty && minSum != 0) ? (double)(AnalyzedTableData[i].min + 114) / minSum : 0;
                a.avgDist = (-113 <= a.avg && a.avg <= -51 && a.lat != string.Empty && a.lon != string.Empty && avgSum != 0) ? (double)(AnalyzedTableData[i].avg + 114) / avgSum : 0;
                a.maxDist = (-113 <= a.max && a.max <= -51 && a.lat != string.Empty && a.lon != string.Empty && maxSum != 0) ? (double)(AnalyzedTableData[i].max + 114) / maxSum : 0;
            }
        }

        public class CellRecords
        {
            public string MCC { get; set; }
            public string MNC { get; set; }
            public List<CellRecordItem> Cells { get; set; }

            public CellRecords()
            {
                MCC = string.Empty;
                MNC = string.Empty;
                Cells = new List<CellRecordItem>();
            }

            public View getFormattedData(int ID)
            {
                LayoutInflater inflater = (LayoutInflater)Android.App.Application.Context.GetSystemService(Context.LayoutInflaterService);
                View table = inflater.Inflate(Resource.Layout.CellCaptureData, null);
                TextView numberField = table.FindViewById<TextView>(Resource.Id.Number_Field);
                numberField.Text = String.Format("#{0}", ID);
                TextView mccField = table.FindViewById<TextView>(Resource.Id.MCC_Field);
                mccField.Text = String.Format("{0}", MCC);
                TextView mncField = table.FindViewById<TextView>(Resource.Id.MNC_Field);
                mncField.Text = String.Format("{0}", MNC);

                TableLayout tableLayout = table.FindViewById<TableLayout>(Resource.Id.CellCaptureTable);


                for (int i = 0; i < Cells.Count; i++)
                {
                    View tableRow = Cells[i].getFormattedData();
                    tableLayout.AddView(tableRow);
                }

                return table;
            }
        }
        public class CellRecordItem
        {
            public int LAC { get; set; }
            public int CellID { get; set; }
            public int RSSI { get; set; }

            public CellRecordItem()
            {
                LAC = -1;
                CellID = NeighboringCellInfo.UnknownCid;
                RSSI = NeighboringCellInfo.UnknownRssi;
            }

            public View getFormattedData()
            {
                LayoutInflater inflater = (LayoutInflater)Android.App.Application.Context.GetSystemService(Context.LayoutInflaterService);
                View tableRow = inflater.Inflate(Resource.Layout.CellDataRow, null);
                TextView LAC_Field = tableRow.FindViewById<TextView>(Resource.Id.LAC_Field);
                LAC_Field.Text = LAC.ToString();
                TextView CellID_Field = tableRow.FindViewById<TextView>(Resource.Id.CellID_Field);
                CellID_Field.Text = CellID != NeighboringCellInfo.UnknownCid ? CellID.ToString() : Unknown;
                TextView RSSI_Field = tableRow.FindViewById<TextView>(Resource.Id.RSSI_Field);
                RSSI_Field.Text = RSSI != NeighboringCellInfo.UnknownRssi ? String.Format("{0} dBm", RSSI) : Unknown;
                return tableRow;
            }
        }

        public class AnalyzeItem
        {
            public string MCC { get; set; }
            public string MNC { get; set; }
            public int LAC { get; set; }
            public int CellID { get; set; }
            public List<int> RSSIs;
            public int min, max;
            public double avg;
            public double minDist, avgDist, maxDist;
            public string lat, lon;

            public AnalyzeItem()
            {
                MCC = string.Empty;
                MNC = string.Empty;
                LAC = -1;
                CellID = NeighboringCellInfo.UnknownCid;
                RSSIs = new List<int>();
                min = -50;
                avg = 0.0;
                max = -114;
                minDist = 0;
                avgDist = 0;
                maxDist = 0;
                lat = string.Empty;
                lon = string.Empty;
            }

            private void getRowsHeaders(View tableRowLayout)
            {
                TextView MCC_Field = tableRowLayout.FindViewById<TextView>(Resource.Id.MCC_Field);
                MCC_Field.Text = MCC.ToString();
                TextView MNC_Field = tableRowLayout.FindViewById<TextView>(Resource.Id.MNC_Field);
                MNC_Field.Text = MNC.ToString();
                TextView LAC_Field = tableRowLayout.FindViewById<TextView>(Resource.Id.LAC_Field);
                LAC_Field.Text = LAC.ToString();
                TextView CellID_Field = tableRowLayout.FindViewById<TextView>(Resource.Id.CellID_Field);
                CellID_Field.Text = CellID != NeighboringCellInfo.UnknownCid ? CellID.ToString() : Unknown;
            }

            public View getRSSIFormattedData()
            {
                findMinAvgMax();
                if (RSSIs.Count != 0)
                {
                    LayoutInflater inflater = (LayoutInflater)Android.App.Application.Context.GetSystemService(Context.LayoutInflaterService);
                    View tableRowLayout = inflater.Inflate(Resource.Layout.AnalyzingRow, null);
                    getRowsHeaders(tableRowLayout);

                    TableRow row = tableRowLayout.FindViewById<TableRow>(Resource.Id.Row);
                    for (int i = 0; i < RSSIs.Count; i++)
                    {
                        row.AddView(createDataField(inflater, RSSIs[i]));
                    }

                    row.AddView(createDataField(inflater, min));
                    row.AddView(createDataField(inflater, avg));
                    row.AddView(createDataField(inflater, max));

                    return tableRowLayout;
                }
                else return null;
            }

            public View getCoordinateFormattedData()
            {
                LayoutInflater inflater = (LayoutInflater)Android.App.Application.Context.GetSystemService(Context.LayoutInflaterService);
                View tableRowLayout = inflater.Inflate(Resource.Layout.AnalyzingRow, null);
                getRowsHeaders(tableRowLayout);

                TableRow row = tableRowLayout.FindViewById<TableRow>(Resource.Id.Row);

                row.AddView(createDataField(inflater, minDist, false));
                row.AddView(createDataField(inflater, avgDist, false));
                row.AddView(createDataField(inflater, maxDist, false));

                View latColumLayout = inflater.Inflate(Resource.Layout.AnalyzingColumn, null);
                TextView latColumn = latColumLayout.FindViewById<TextView>(Resource.Id.Column);
                latColumn.Text = (lat != string.Empty) ? lat : noData;
                latColumn.SetTextColor(Android.Graphics.Color.Black);
                row.AddView(latColumLayout);

                View lonColumLayout = inflater.Inflate(Resource.Layout.AnalyzingColumn, null);
                TextView lonColumn = lonColumLayout.FindViewById<TextView>(Resource.Id.Column);
                lonColumn.Text = (lon != string.Empty) ? lon : noData;
                lonColumn.SetTextColor(Android.Graphics.Color.Black);
                row.AddView(lonColumLayout);

                return tableRowLayout;
            }

            private View createDataField(LayoutInflater inflater, double value, bool check = true)
            {
                View tableColumnLayout = inflater.Inflate(Resource.Layout.AnalyzingColumn, null);
                TextView column = tableColumnLayout.FindViewById<TextView>(Resource.Id.Column);
                column.Text = (check) ? ((-51 >= value && value >= -113) ? value.ToString() : noData) : value.ToString();
                column.SetTextColor(Android.Graphics.Color.Black);
                return tableColumnLayout;
            }

            public void findMinAvgMax()
            {
                if (RSSIs.Count != 0)
                {
                    int sum = 0;
                    int count = 0;
                    min = -50;
                    max = -114;
                    for (int i = 0; i < RSSIs.Count; i++)
                    {
                        if (-113 <= RSSIs[i] && RSSIs[i] <= -51)
                        {
                            sum += RSSIs[i];
                            count++;
                            min = Math.Min(min, RSSIs[i]);
                            max = Math.Max(max, RSSIs[i]);
                        }
                    }
                    if (count != 0)
                        avg = (double)sum / (double)count;
                    else
                        avg = 0;
                }
            }

            public void fetchCoordinates()
            {
                if (lat == string.Empty || lon == string.Empty)
                {
                    if (MCC != string.Empty && MNC != string.Empty && LAC != -1 && CellID != NeighboringCellInfo.UnknownCid)
                    {
                        int mcc_value = -1, mnc_value = -1;
                        if (Int32.TryParse(MCC, out mcc_value) && Int32.TryParse(MNC, out mnc_value))
                        {
                            string latLon = GMM.GetLatLng(mcc_value, mnc_value, LAC, CellID);
                            if (latLon != string.Empty)
                            {
                                lat = latLon.Substring(0, latLon.IndexOf("|"));
                                lon = latLon.Substring(latLon.IndexOf("|") + 1);
                            }
                        }
                    }
                }
            }
        }

    }

    [Activity(Label = "Triangulation", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : TabActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            Data.noData = Resources.GetString(Resource.String.NoData);
            Data.Unknown = Resources.GetString(Resource.String.UnknownString);


            CreateTab(typeof(Scannig), Resources.GetString(Resource.String.ScanTab), Resources.GetString(Resource.String.ScanTab), Resource.Drawable.scan);
            CreateTab(typeof(Analyzing), Resources.GetString(Resource.String.AnalyzeTab), Resources.GetString(Resource.String.AnalyzeTab), Resource.Drawable.analyze);
            CreateTab(typeof(Coordinates), Resources.GetString(Resource.String.ResultTab), Resources.GetString(Resource.String.ResultTab), Resource.Drawable.result);

            Button pref = FindViewById<Button>(Resource.Id.Pref);
            pref.Click += delegate
            {
                showNetworkSettings();
            };

        }

        public void showNetworkSettings()
        {
            Intent intent = new Intent(Android.Provider.Settings.ActionSettings);
            StartActivity(intent);
        }

        private void CreateTab(Type activityType, string tag, string label, int drawableId)
        {
            var intent = new Intent(this, activityType);
            intent.AddFlags(ActivityFlags.NewTask);

            var spec = TabHost.NewTabSpec(tag);
            var drawableIcon = Resources.GetDrawable(drawableId);
            spec.SetIndicator(label, drawableIcon);
            spec.SetContent(intent);

            TabHost.AddTab(spec);
        }
    }

    [Activity]
    public class Scannig : Activity
    {
        private const int waitingSeconds = 10;
        private const int CapturingSeconds = 60;
        private int networkChangeSleeping = 0;
        private const int scanIntervalInMilliseconds = 1000;

        TelephonyManager telephonyManager;
        TextView progress;
        private volatile int remainigSeconds = -1;
        private System.Timers.Timer timer;
        int mainCellId = NeighboringCellInfo.UnknownCid, mainLac;
        MyPhoneStateListener phoneStateListener;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Scanning);

            telephonyManager = (TelephonyManager)GetSystemService(Context.TelephonyService);
            progress = FindViewById<TextView>(Resource.Id.ProgressIndicator);

            Button button = FindViewById<Button>(Resource.Id.StartButton);
            button.Click += delegate
            {
                if (telephonyManager.NetworkType != NetworkType.Gprs && telephonyManager.NetworkType != NetworkType.Edge)
                {
                    AlertDialog.Builder alert = new AlertDialog.Builder(this);
                    alert.SetTitle(Resources.GetString(Resource.String.NoGSMTitle));
                    alert.SetMessage(Resources.GetString(Resource.String.NoGSMBody));
                    alert.SetPositiveButton(Resources.GetString(Resource.String.OK_Button), (senderAlert, args) =>
                    {
                        showNetworkSettings();
                    });
                    Dialog alertDialog = alert.Create();
                    alertDialog.SetCanceledOnTouchOutside(false);
                    alertDialog.Show();
                }
                else
                {
                    Button thisButton = FindViewById<Button>(Resource.Id.StartButton);
                    thisButton.Text = Resources.GetString(Resource.String.ScannigInProgress);
                    thisButton.Enabled = false;

                    LinearLayout l = FindViewById<LinearLayout>(Resource.Id.DataLayout);
                    l.RemoveAllViews();

                    remainigSeconds = waitingSeconds;
                    updateProgressUi(Resources.GetString(Resource.String.PreparationString), remainigSeconds);
                    timer = new System.Timers.Timer();
                    timer.Interval = scanIntervalInMilliseconds;
                    timer.Elapsed += OnTimedEvent;
                    timer.Enabled = true;
                }
            };

            int count = 0;
            GsmCellLocation gsmLocation = (GsmCellLocation)telephonyManager.CellLocation;
            if (gsmLocation != null)
            {
                while (mainCellId == NeighboringCellInfo.UnknownCid)
                {
                    mainCellId = gsmLocation.Cid;
                    mainLac = gsmLocation.Lac;
                    if (++count > 10)
                        showCloseAllert();
                }
            }
            else
                showCloseAllert();

            phoneStateListener = new MyPhoneStateListener();
            telephonyManager.Listen(phoneStateListener, PhoneStateListenerFlags.SignalStrengths);
        }
        protected override void OnStop()
        {
            telephonyManager.Listen(phoneStateListener, PhoneStateListenerFlags.None);
            base.OnStop();
        }

        public void showNetworkSettings()
        {
            Intent intent = new Intent(Android.Provider.Settings.ActionSettings);
            StartActivity(intent);
        }
        private void updateProgressUi(string msg, int time)
        {
            RunOnUiThread(() =>
            {
                progress.Text = String.Format("{0} {1}...", msg, time);
            });
        }
        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            updateProgressUi(Resources.GetString(Resource.String.PreparationString), remainigSeconds);
            if (--remainigSeconds <= 0)
            {
                timer.Stop();
                remainigSeconds = CapturingSeconds;
                Data.listOfRecords = new List<Data.CellRecords>();
                updateProgressUi(Resources.GetString(Resource.String.CaptureString), remainigSeconds);
                timer = new System.Timers.Timer();
                timer.Interval = scanIntervalInMilliseconds;
                timer.Elapsed += OnCapturingEvent;
                timer.Enabled = true;
            }
        }
        private void showCloseAllert()
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle(Resources.GetString(Resource.String.ScanFailureTitle));
            alert.SetMessage(Resources.GetString(Resource.String.ScanFailureBody));
            alert.SetPositiveButton(Resources.GetString(Resource.String.OK_Button), (senderAlert, args) => { alert.Dispose(); });
            RunOnUiThread(() =>
            {
                alert.Show();
            });
        }

        [BroadcastReceiver]
        private class MyPhoneStateListener : PhoneStateListener
        {
            public int RSSI { get; private set; }
            public MyPhoneStateListener()
            {
                RSSI = NeighboringCellInfo.UnknownRssi;
            }

            public override void OnSignalStrengthsChanged(SignalStrength signalStrength)
            {
                base.OnSignalStrengthsChanged(signalStrength);
                RSSI = signalStrength.GsmSignalStrength;
            }
        }

        private void OnCapturingEvent(object sender, System.Timers.ElapsedEventArgs e)
        {

            if (networkChangeSleeping == 0)
            {
                RunOnUiThread(() =>
                {
                    getNeighborCells();
                });
                if (--remainigSeconds <= 0)
                {
                    timer.Stop();
                    RunOnUiThread(() =>
                    {
                        Button thisButton = FindViewById<Button>(Resource.Id.StartButton);
                        thisButton.Text = Resources.GetString(Resource.String.StartButton);
                        thisButton.Enabled = true;
                    });
                }
                updateProgressUi(Resources.GetString(Resource.String.CaptureString), remainigSeconds);
            }
            else
            {
                updateProgressUi(String.Format(Resources.GetString(Resource.String.NetworkChanged), networkChangeSleeping), networkChangeSleeping);
                networkChangeSleeping--;
            }

            
        }

        private void getNeighborCells()
        {
            Data.CellRecords record = new Data.CellRecords(); //овде се чуваат сите скенирани резултати

            // се добива кодот за државата и операторот и се запишува во соодветната променлива
            string MCC_MNC = telephonyManager.NetworkOperator;
            if (MCC_MNC != null)
            {
                record.MCC = MCC_MNC.Substring(0, 3);
                record.MNC = MCC_MNC.Substring(3);
            }
            else return;

            //се земаат податоците за моменталната станица
            GsmCellLocation gsmLocation = (GsmCellLocation)telephonyManager.CellLocation;
            if (gsmLocation != null)
            {
                Data.CellRecordItem item = new Data.CellRecordItem();
                item.CellID = gsmLocation.Cid; // идентификација на ќелијата
                item.LAC = gsmLocation.Lac; //локален код за регионот на ќелијата

                //проверуваме дали случајно сме се поврзале на друга ќелија, и ако да 
                //чекаме 5 секунди за да може да се обноват податоците за околките ќелии
                if (item.CellID != mainCellId || item.LAC != mainLac)
                {
                    timer.Enabled = false;
                    networkChangeSleeping = 5;
                    mainCellId = item.CellID;
                    mainLac = item.LAC;
                    remainigSeconds++;
                    timer.Enabled = true;
                    return;
                }

                //проверуваме дали идентификацијата ќелијата и сигналот се валидни
                if (item.CellID != NeighboringCellInfo.UnknownCid &&
                    phoneStateListener.RSSI != NeighboringCellInfo.UnknownRssi)
                {
                    //за јачината на сигналот кај примарната станица се користи 
                    //поразлична методата за разлика од кај соседните
                    item.RSSI = -113 + 2 * phoneStateListener.RSSI;
                    record.Cells.Add(item);
                }
                else return;
            }
            else return;

            //ја преземаме листата за соседни станици
            IList<NeighboringCellInfo> listNeigborCells = telephonyManager.NeighboringCellInfo;
            if (listNeigborCells != null)
            {
                //за секоја станица ја повторуваме истата постапка од претхосно
                for (int i = 0; i < listNeigborCells.Count; i++)
                {
                    NeighboringCellInfo cellinfo = listNeigborCells[i];
                    int networkType = cellinfo.NetworkType;

                    //се врши проверка дали станицата е од тип GPRS или EDGE (и двата спаѓаат во GSM)
                    if (networkType == (int)NetworkType.Gprs || networkType == (int)NetworkType.Edge)
                    {
                        Data.CellRecordItem item = new Data.CellRecordItem();
                        item.CellID = cellinfo.Cid; item.LAC = cellinfo.Lac;
                        int rssi = cellinfo.Rssi;   //јачината на сигналот

                        if (item.CellID != NeighboringCellInfo.UnknownCid &&
                            rssi != NeighboringCellInfo.UnknownRssi)
                        {
                            item.RSSI = -113 + 2 * rssi; record.Cells.Add(item);
                        }
                    }
                }

                //Доколку сме нашле барем една станица, ја зачувуваме
                if (record.Cells.Count != 0)
                {
                    Data.listOfRecords.Add(record); int index = -1;
                    for (int i = Data.listOfRecords.Count - 1; i >= 0; i--)
                    {
                        if (Data.listOfRecords[i] == record)
                        {
                            index = i; break;
                        }
                    }
                    if (index != -1)
                    {
                        LinearLayout l = FindViewById<LinearLayout>(Resource.Id.DataLayout);
                        l.AddView(record.getFormattedData(index + 1));
                    }
                }
            }
        }

    }

    [Activity]
    public class Analyzing : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Analyzing);

            Button button = FindViewById<Button>(Resource.Id.AnalyzingButton);
            button.Click += delegate
            {
                if (Data.listOfRecords == null)
                    ShowNoDataDialog();
                else if (Data.listOfRecords.Count == 0)
                    ShowNoDataDialog();
                else
                {
                    Button thisButton = FindViewById<Button>(Resource.Id.AnalyzingButton);
                    thisButton.Enabled = false;

                    LinearLayout l = FindViewById<LinearLayout>(Resource.Id.AnalyzingContent);
                    l.RemoveAllViews();

                    RunOnUiThread(() =>
                    {
                        TextView txt = FindViewById<TextView>(Resource.Id.AnalyzingProgress);
                        txt.Text = Resources.GetString(Resource.String.AnalyzingProgess);
                    });

                    AnalyzeData();

                }
            };
        }

        private void ShowNoDataDialog()
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle(Resources.GetString(Resource.String.NoDataTitle));
            alert.SetMessage(Resources.GetString(Resource.String.NoDataBody));
            alert.SetNeutralButton(Resources.GetString(Resource.String.OK_Button), (senderAlert, args) => { alert.Dispose(); });
            RunOnUiThread(() =>
            {
                alert.Show();
            });
        }

        private void AnalyzeData()
        {
            Data.AnalyzeData();

            LayoutInflater inflater = (LayoutInflater)Android.App.Application.Context.GetSystemService(Context.LayoutInflaterService);
            View table = inflater.Inflate(Resource.Layout.AnalyzingTable, null);
            TextView tableLabel = table.FindViewById<TextView>(Resource.Id.TableLabel);
            tableLabel.Text = Resources.GetString(Resource.String.RSSIValues);

            TableRow header = table.FindViewById<TableRow>(Resource.Id.HeaderRow);

            for (int i = 0; i < Data.listOfRecords.Count; i++)
            {
                header.AddView(ColumnLayoutCreator(inflater, String.Format("#{0}", i + 1)));
            }

            header.AddView(ColumnLayoutCreator(inflater, Resources.GetString(Resource.String.minimum)));
            header.AddView(ColumnLayoutCreator(inflater, Resources.GetString(Resource.String.average)));
            header.AddView(ColumnLayoutCreator(inflater, Resources.GetString(Resource.String.maximum)));


            TableLayout tableLayout = table.FindViewById<TableLayout>(Resource.Id.Table);

            for (int i = 0; i < Data.AnalyzedTableData.Count; i++)
            {
                tableLayout.AddView(Data.AnalyzedTableData[i].getRSSIFormattedData());
            }

            LinearLayout analyzingContent = FindViewById<LinearLayout>(Resource.Id.AnalyzingContent);
            analyzingContent.AddView(table);

            Button thisButton = FindViewById<Button>(Resource.Id.AnalyzingButton);
            thisButton.Enabled = true;

            RunOnUiThread(() =>
            {
                TextView txt = FindViewById<TextView>(Resource.Id.AnalyzingProgress);
                txt.Text = Resources.GetString(Resource.String.AnalyzingDone);
            });
        }

        private View ColumnLayoutCreator(LayoutInflater inflater, String txt)
        {
            View tableColumnLayout = inflater.Inflate(Resource.Layout.AnalyzingColumn, null);
            TextView column = tableColumnLayout.FindViewById<TextView>(Resource.Id.Column);
            column.Text = txt;
            return tableColumnLayout;
        }
    }

    [Activity]
    public class Coordinates : Activity, ILocationListener
    {
        private Button fetch, calc, getGps, expData;
        private TextView progressIndicator, locationLat, locationLon;
        private bool hasCalculated = false, hasReal = false;
        private LocationManager locationManager;
        private string locationProvider;
        private Location currentLocation;
        private bool updateGPS;
        private double minX = 0, minY = 0, avgX = 0, avgY = 0, maxX = 0, maxY = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Coordinates);

            fetch = FindViewById<Button>(Resource.Id.TryFetch);
            calc = FindViewById<Button>(Resource.Id.CalculateLocation);
            getGps = FindViewById<Button>(Resource.Id.GetGPSCoordinates);
            expData = FindViewById<Button>(Resource.Id.ExportData);

            progressIndicator = FindViewById<TextView>(Resource.Id.FinalResultProgress);
            locationLat = FindViewById<TextView>(Resource.Id.GPSLat);
            locationLon = FindViewById<TextView>(Resource.Id.GPSLon);

            updateGPS = false;
            InitializeLocationManager();

            fetch.Click += delegate
            {
                if (Data.AnalyzedTableData == null)
                    ShowWarningDialog(Resources.GetString(Resource.String.NoDataCollectedForFetchingTitle), Resources.GetString(Resource.String.NoDataCollectedForFetchingBody));
                else if (Data.AnalyzedTableData.Count == 0)
                    ShowWarningDialog(Resources.GetString(Resource.String.NoDataCollectedForFetchingTitle), Resources.GetString(Resource.String.NoDataCollectedForFetchingBody));
                else
                {
                    if (CheckForConnection())
                    {
                        ToggleAllButtons(false);
                        FetchCoordinates();
                    }
                    else
                    {
                        ShowWarningDialog(Resources.GetString(Resource.String.NoConnectionTitle), Resources.GetString(Resource.String.NoConnectionBody));
                    }

                }
            };
            calc.Click += delegate
            {
                if (Data.AnalyzedTableData == null)
                    ShowWarningDialog(Resources.GetString(Resource.String.NoDataCollectedForFetchingTitle), Resources.GetString(Resource.String.NoDataCollectedForFetchingBody));
                else if (Data.AnalyzedTableData.Count == 0)
                    ShowWarningDialog(Resources.GetString(Resource.String.NoDataCollectedForFetchingTitle), Resources.GetString(Resource.String.NoDataCollectedForFetchingBody));
                else
                {
                    bool check = false;
                    for (int i = 0; i < Data.AnalyzedTableData.Count; i++)
                    {
                        if (Data.AnalyzedTableData[i].lat != string.Empty && Data.AnalyzedTableData[i].lon != string.Empty)
                        {
                            check = true; break;
                        }
                    }

                    if (!check)
                        ShowWarningDialog(Resources.GetString(Resource.String.PrecalculationTitle), Resources.GetString(Resource.String.PrecalculationBody));
                    else
                        MakeCalculations();
                }
            };
            getGps.Click += delegate
            {                    
                if (locationProvider != String.Empty)
                {
                    getGps.Text = Resources.GetString((updateGPS) ? Resource.String.GetGPSCoordinates : Resource.String.GetGPSCoordinatesStop);
                    updateGPS = !updateGPS;
                }
                else
                {
                    getGps.Text = Resources.GetString(Resource.String.GetGPSCoordinates);
                    updateGPS = false;
                }
            };
            expData.Click += delegate
            {
                if (Data.listOfRecords == null)
                    ShowWarningDialog(Resources.GetString(Resource.String.ExportFailure), Resources.GetString(Resource.String.ExportNoData));
                else if (Data.AnalyzedTableData == null)
                    ShowWarningDialog(Resources.GetString(Resource.String.ExportFailure), Resources.GetString(Resource.String.ExportNotAnalyzed));
                else if (Data.AnalyzedTableData.Count == 0)
                    ShowWarningDialog(Resources.GetString(Resource.String.ExportFailure), Resources.GetString(Resource.String.ExportNotAnalyzed));
                else
                {
                    bool hascoord = false, continues = true;
                    for (int i = 0; i < Data.AnalyzedTableData.Count; i++)
                    {
                        if (Data.AnalyzedTableData[i].lat != String.Empty && Data.AnalyzedTableData[i].lon != String.Empty)
                        {
                            hascoord = true; break;
                        }
                    }

                    if (!hascoord || currentLocation == null)
                    {
                        AlertDialog.Builder alert = new AlertDialog.Builder(this);
                        alert.SetTitle(Resources.GetString(Resource.String.ExportFailure));
                        alert.SetMessage(Resources.GetString(Resource.String.ExportNoCoordinates));
                        alert.SetPositiveButton(Resources.GetString(Resource.String.YES_BUTTON), (senderAlert, args) => { continues = true; });
                        alert.SetNegativeButton(Resources.GetString(Resource.String.NO_BUTTON), (senderAlert, args) => { continues = false; });
                        Dialog alertDialog = alert.Create();
                        alertDialog.SetCanceledOnTouchOutside(false);
                        alertDialog.Show();
                    }

                    if (continues)
                    {
                        updateGPS = false;
                        ExtractToCSV();
                    }
                }
            };
        }

        private void ExtractToCSV()
        {
            RunOnUiThread(() =>
            {
                progressIndicator.Text = Resources.GetString(Resource.String.ExportStarted);
            });

            string s = "MCC,MNC,LAC,CID";
            for (int i = 0; i < Data.listOfRecords.Count; i++)
                s = String.Format("{0},#{1}", s, i);
            s = String.Format("{0},minValue,avgValue,maxValue,minDistro,avgDistro,maxDistro,latitude,longitude", s);

            for (int i = 0; i < Data.AnalyzedTableData.Count; i++)
            {
                Data.AnalyzeItem a = Data.AnalyzedTableData[i];
                s = String.Format("{0}\n{1},{2},{3},{4}", s, a.MCC, a.MNC, a.LAC, a.CellID);
                for (int j = 0; j < a.RSSIs.Count; j++)
                    s = String.Format("{0},{1}", s, ((-113 <= a.RSSIs[j] && a.RSSIs[j] <= -51) ? a.RSSIs[j].ToString() : "-"));
                s = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", s,
                    ((-113 <= a.min && a.min <= -51) ? a.min.ToString() : "*"),
                    ((-113 <= a.avg && a.avg <= -51) ? a.avg.ToString() : "*"),
                    ((-113 <= a.max && a.max <= -51) ? a.max.ToString() : "*"),
                    ((0 <= a.minDist && a.minDist <= 1) ? a.minDist.ToString() : "*"),
                    ((0 <= a.avgDist && a.avgDist <= 1) ? a.avgDist.ToString() : "*"),
                    ((0 <= a.maxDist && a.maxDist <= 1) ? a.maxDist.ToString() : "*"),
                    a.lat, a.lon);
            }

            s = String.Format("{0}\n\n*,minLocation,avgLocation,maxLocation,GPSLocation\nLatitude,{1},{2},{3},{4}\nLongitude,{5},{6},{7},{8}\nDifference (m),{9},{10},{11},*", s, hasCalculated ? minX.ToString() : "*",
                hasCalculated ? avgX.ToString() : "*",
                hasCalculated ? maxX.ToString() : "*",
                hasReal ? currentLocation.Latitude.ToString() : "-",
                hasCalculated ? minY.ToString() : "*",
                hasCalculated ? avgY.ToString() : "*",
                hasCalculated ? maxY.ToString() : "*",
                hasReal ? currentLocation.Longitude.ToString() : "*",
                hasCalculated && hasReal ? CalculateDistance(minX, minY, currentLocation.Latitude, currentLocation.Longitude).ToString() : "*",
                hasCalculated && hasReal ? CalculateDistance(avgX, avgY, currentLocation.Latitude, currentLocation.Longitude).ToString() : "*",
                hasCalculated && hasReal ? CalculateDistance(maxX, maxY, currentLocation.Latitude, currentLocation.Longitude).ToString() : "*");

            var sdCardPath = Android.OS.Environment.ExternalStorageDirectory.Path;
            DateTime d = System.DateTime.Now;
            String fileName = String.Format("Scan_{0}_{1}_{2}_{3}_{4}_{5}_{6}.csv",
                new Random().Next(100000, 999999), d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second);
            var filePath = System.IO.Path.Combine(sdCardPath, fileName);
            if (!System.IO.File.Exists(filePath))
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath, true))
                {
                    writer.Write(s);
                }
            }

            RunOnUiThread(() =>
            {
                progressIndicator.Text = String.Format("{1}:\n{0}", filePath, Resources.GetString(Resource.String.ExportEnded));
            });
        }

        protected override void OnResume()
        {
            base.OnResume();
            InitializeLocationManager();
            if (locationProvider != string.Empty)
                locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);
        }

        protected override void OnPause()
        {
            base.OnPause();
            locationManager.RemoveUpdates(this);
        }

        private void ShowWarningDialog(String title, String body)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle(title);
            alert.SetMessage(body);
            alert.SetNeutralButton(Resources.GetString(Resource.String.OK_Button), (senderAlert, args) => { alert.Dispose(); });
            RunOnUiThread(() =>
            {
                alert.Show();
            });
        }

        private void ToggleAllButtons(bool enabled)
        {
            fetch.Enabled = enabled;
            calc.Enabled = enabled;
            getGps.Enabled = enabled;
            expData.Enabled = enabled;
        }

        private bool CheckForConnection()
        {
            ConnectivityManager connectivityManager = (ConnectivityManager)GetSystemService(ConnectivityService);
            NetworkInfo activeConnection = connectivityManager.ActiveNetworkInfo;
            return (activeConnection != null) && activeConnection.IsConnected;
        }

        private void FetchCoordinates()
        {
            RunOnUiThread(() =>
            {
                progressIndicator.Text = Resources.GetString(Resource.String.FetchingStarted);
            });
            Data.TryFetchingCellCoordinates();

            LinearLayout l = FindViewById<LinearLayout>(Resource.Id.CellCoordinatesContent);
            l.RemoveAllViews();

            LayoutInflater inflater = (LayoutInflater)Android.App.Application.Context.GetSystemService(Context.LayoutInflaterService);
            View table = inflater.Inflate(Resource.Layout.AnalyzingTable, null);
            TextView tableLabel = table.FindViewById<TextView>(Resource.Id.TableLabel);
            tableLabel.Text = "Distributions and Cell's coordinates";

            TableRow header = table.FindViewById<TableRow>(Resource.Id.HeaderRow);
            header.AddView(ColumnLayoutCreator(inflater, "min distro"));
            header.AddView(ColumnLayoutCreator(inflater, "avg distro"));
            header.AddView(ColumnLayoutCreator(inflater, "max distro"));
            header.AddView(ColumnLayoutCreator(inflater, "latitude"));
            header.AddView(ColumnLayoutCreator(inflater, "longitude"));

            TableLayout tableLayout = table.FindViewById<TableLayout>(Resource.Id.Table);

            for (int i = 0; i < Data.AnalyzedTableData.Count; i++)
            {
                tableLayout.AddView(Data.AnalyzedTableData[i].getCoordinateFormattedData());
            }

            l.AddView(table);

            ToggleAllButtons(true);
            RunOnUiThread(() =>
            {
                progressIndicator.Text = Resources.GetString(Resource.String.FetchingDone);
            });
        }

        private void MakeCalculations()
        {
            minX = 0; minY = 0; avgX = 0; avgY = 0; maxX = 0; maxY = 0;
            int countTrue = 0;
            for (int i = 0; i < Data.AnalyzedTableData.Count; i++)
            {
                Data.AnalyzeItem a = Data.AnalyzedTableData[i];
                double lat, lon;
                if (Double.TryParse(a.lat, out lat) && Double.TryParse(a.lon, out lon))
                {
                    countTrue++;
                    minX += a.minDist * lat; minY += a.minDist * lon;
                    avgX += a.avgDist * lat; avgY += a.avgDist * lon;
                    maxX += a.maxDist * lat; maxY += a.maxDist * lon;
                }
            }

            if (countTrue != 0)
                hasCalculated = true;
            else
                hasCalculated = false;

            SetText(countTrue != 0 ? minX.ToString() : "unknown", Resource.Id.minimumLat);
            SetText(countTrue != 0 ? minY.ToString() : "unknown", Resource.Id.minimumLon);
            SetText(countTrue != 0 ? avgX.ToString() : "unknown", Resource.Id.averageLat);
            SetText(countTrue != 0 ? avgY.ToString() : "unknown", Resource.Id.averageLon);
            SetText(countTrue != 0 ? maxX.ToString() : "unknown", Resource.Id.maximumLat);
            SetText(countTrue != 0 ? maxY.ToString() : "unknown", Resource.Id.maximumLon);

            updateDistanceValues();
        }

        private void SetText(String text, int id)
        {
            TextView t = FindViewById<TextView>(id);
            t.Text = text;
        }

        private View ColumnLayoutCreator(LayoutInflater inflater, String txt)
        {
            View tableColumnLayout = inflater.Inflate(Resource.Layout.AnalyzingColumn, null);
            TextView column = tableColumnLayout.FindViewById<TextView>(Resource.Id.Column);
            column.Text = txt;
            return tableColumnLayout;
        }

        private void InitializeLocationManager()
        {
            locationManager = (LocationManager)GetSystemService(LocationService);
            if (!locationManager.IsProviderEnabled(LocationManager.GpsProvider) || !locationManager.IsProviderEnabled(LocationManager.NetworkProvider))
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetTitle("Location Service Not Active");
                alert.SetMessage("Please enable location service and GPS");
                alert.SetPositiveButton("OK", (senderAlert, args) =>
                {
                    Intent intent = new Intent(Android.Provider.Settings.ActionLocationSourceSettings);
                    StartActivity(intent);
                });
                Dialog alertDialog = alert.Create();
                alertDialog.SetCanceledOnTouchOutside(false);
                alertDialog.Show();
            }

            Criteria criteriaForLocationService = new Criteria();
            criteriaForLocationService.Accuracy = Accuracy.Fine;

            IList<string> acceptablelocationProviders = locationManager.GetProviders(criteriaForLocationService, true);

            if (acceptablelocationProviders.Any())
            {
                locationProvider = acceptablelocationProviders.First();
                if (locationProvider != string.Empty)
                    locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);
            }
            else
                locationProvider = string.Empty;
        }

        public void OnLocationChanged(Location location)
        {
            if (updateGPS)
            {
                currentLocation = location;
                if (currentLocation == null)
                {
                    locationLat.Text = "...";
                    locationLon.Text = "...";
                    hasReal = false;
                }
                else
                {
                    locationLat.Text = string.Format("{0}", currentLocation.Latitude);
                    locationLon.Text = string.Format("{0}", currentLocation.Longitude);
                    hasReal = true;
                }
                updateDistanceValues();
            }
        }

        public void OnProviderDisabled(string provider)
        {
            if (provider == "gps")
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetTitle("Location Service Not Active");
                alert.SetMessage("Please enable location service and GPS");
                alert.SetPositiveButton("OK", (senderAlert, args) =>
                {
                    Intent intent = new Intent(Android.Provider.Settings.ActionLocationSourceSettings);
                    StartActivity(intent);
                });
                Dialog alertDialog = alert.Create();
                alertDialog.SetCanceledOnTouchOutside(false);
                alertDialog.Show();
            }
        }

        public void OnProviderEnabled(string provider)
        {
            return;
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            return;
        }

        private void updateDistanceValues()
        {
            TextView minE = FindViewById<TextView>(Resource.Id.minimumError);
            TextView avgE = FindViewById<TextView>(Resource.Id.averageError);
            TextView maxE = FindViewById<TextView>(Resource.Id.maximumError);

            if (hasReal && hasCalculated && currentLocation != null)
            {
                minE.Text = CalculateDistance(minX, minY, currentLocation.Latitude, currentLocation.Longitude).ToString();
                avgE.Text = CalculateDistance(avgX, avgY, currentLocation.Latitude, currentLocation.Longitude).ToString();
                maxE.Text = CalculateDistance(maxX, maxY, currentLocation.Latitude, currentLocation.Longitude).ToString();
            }
            else
            {
                minE.Text = "...";
                avgE.Text = "...";
                maxE.Text = "...";
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371e3; //metres
            double rLat1 = (Math.PI / 180) * lat1;
            double rLat2 = (Math.PI / 180) * lat2;
            double rDeltaLat = (Math.PI / 180) * (lat2 - lat1);
            double rDeltaLon = (Math.PI / 180) * (lon1 - lon2);

            double a = Math.Pow(Math.Sin(rDeltaLat / 2), 2.0) + Math.Cos(rLat1) * Math.Cos(rLat2) * Math.Pow(Math.Sin(rDeltaLon / 2), 2.0);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = R * c;

            return d;
        }
    }

    class GMM
    {
        static byte[] PostData(int MCC, int MNC, int LAC, int CID, bool shortCID)
        {
            /* The shortCID parameter follows heuristic experiences:
             * Sometimes UMTS CIDs are build up from the original GSM CID (lower 4 hex digits)
             * and the RNC-ID left shifted into the upper 4 digits.
             */
            byte[] pd = new byte[] {
                0x00, 0x0e,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,

                0x1b,
                0x00, 0x00, 0x00, 0x00, // Offset 0x11
                0x00, 0x00, 0x00, 0x00, // Offset 0x15
                0x00, 0x00, 0x00, 0x00, // Offset 0x19
                0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, // Offset 0x1f
                0x00, 0x00, 0x00, 0x00, // Offset 0x23
                0x00, 0x00, 0x00, 0x00, // Offset 0x27
                0x00, 0x00, 0x00, 0x00, // Offset 0x2b
                0xff, 0xff, 0xff, 0xff,
                0x00, 0x00, 0x00, 0x00
            };

            bool isUMTSCell = ((Int64)CID > 65535);

            if (isUMTSCell)
                Console.WriteLine("UMTS CID. {0}", shortCID ?
                    "Using short CID to resolve." : string.Empty);
            else
                Console.WriteLine("GSM CID given.");

            if (shortCID)
                CID &= 0xFFFF;      /* Attempt to resolve the cell using the 
                                    GSM CID part */

            if ((Int64)CID > 65536) /* GSM: 4 hex digits, UTMS: 6 hex 
                                    digits */
                pd[0x1c] = 5;
            else
                pd[0x1c] = 3;

            pd[0x11] = (byte)((MNC >> 24) & 0xFF);
            pd[0x12] = (byte)((MNC >> 16) & 0xFF);
            pd[0x13] = (byte)((MNC >> 8) & 0xFF);
            pd[0x14] = (byte)((MNC >> 0) & 0xFF);

            pd[0x15] = (byte)((MCC >> 24) & 0xFF);
            pd[0x16] = (byte)((MCC >> 16) & 0xFF);
            pd[0x17] = (byte)((MCC >> 8) & 0xFF);
            pd[0x18] = (byte)((MCC >> 0) & 0xFF);

            pd[0x27] = (byte)((MNC >> 24) & 0xFF);
            pd[0x28] = (byte)((MNC >> 16) & 0xFF);
            pd[0x29] = (byte)((MNC >> 8) & 0xFF);
            pd[0x2a] = (byte)((MNC >> 0) & 0xFF);

            pd[0x2b] = (byte)((MCC >> 24) & 0xFF);
            pd[0x2c] = (byte)((MCC >> 16) & 0xFF);
            pd[0x2d] = (byte)((MCC >> 8) & 0xFF);
            pd[0x2e] = (byte)((MCC >> 0) & 0xFF);

            pd[0x1f] = (byte)((CID >> 24) & 0xFF);
            pd[0x20] = (byte)((CID >> 16) & 0xFF);
            pd[0x21] = (byte)((CID >> 8) & 0xFF);
            pd[0x22] = (byte)((CID >> 0) & 0xFF);

            pd[0x23] = (byte)((LAC >> 24) & 0xFF);
            pd[0x24] = (byte)((LAC >> 16) & 0xFF);
            pd[0x25] = (byte)((LAC >> 8) & 0xFF);
            pd[0x26] = (byte)((LAC >> 0) & 0xFF);

            return pd;
        }

        static public string GetLatLng(int MCC, int MNC, int LAC, int CID, bool shortCID = false)
        {
            try
            {
                String url = "http://www.google.com/glm/mmap";
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(new System.Uri(url));
                req.Method = "POST";

                byte[] pd = PostData(MCC, MNC, LAC, CID, shortCID);

                req.ContentLength = pd.Length;
                req.ContentType = "application/binary";
                Stream outputStream = req.GetRequestStream();
                outputStream.Write(pd, 0, pd.Length);
                outputStream.Close();

                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                byte[] ps = new byte[res.ContentLength];
                int totalBytesRead = 0;
                while (totalBytesRead < ps.Length)
                {
                    totalBytesRead += res.GetResponseStream().Read(ps, totalBytesRead, ps.Length - totalBytesRead);
                }

                if (res.StatusCode == HttpStatusCode.OK)
                {
                    short opcode1 = (short)(ps[0] << 8 | ps[1]);
                    byte opcode2 = ps[2];
                    int ret_code = (int)((ps[3] << 24) | (ps[4] << 16) | (ps[5] << 8) | (ps[6]));
                    if (ret_code == 0)
                    {
                        double lat = ((double)((ps[7] << 24) | (ps[8] << 16) | (ps[9] << 8) | (ps[10]))) / 1000000;
                        double lon = ((double)((ps[11] << 24) | (ps[12] << 16) | (ps[13] << 8) | (ps[14]))) / 1000000;
                        return lat + "|" + lon;
                    }
                    else
                        return string.Empty;
                }
                else
                    return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

    }
}

