using NMBCD.Controller;
using NMBCD.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NMBCD.App
{
    public partial class mainForm : Form
    {
        private int intOriginalExStyle = -1;
        private bool bEnableAntiFlicker = true;
        private List<Invoice> _invoiceLst;
        private readonly string _ftpHost;
        private readonly string _ftpUser;
        private readonly string _ftpPass;
        private readonly string _ftpExpRoot;
        private readonly string _ftpImpRoot;
        private readonly string _dbConnection;
        private readonly string _dbEdidata1;

        protected override CreateParams CreateParams
        {
            get
            {
                if (intOriginalExStyle == -1)
                {
                    intOriginalExStyle = base.CreateParams.ExStyle;
                }
                CreateParams cp = base.CreateParams;

                if (bEnableAntiFlicker)
                {
                    cp.ExStyle |= 0x02000000; //WS_EX_COMPOSITED
                }
                else
                {
                    cp.ExStyle = intOriginalExStyle;
                }

                return cp;
            }
        }

        public mainForm()
        {
            ToggleAntiFlicker(false);
            InitializeComponent();

            this.ResizeBegin += Form1_ResizeBegin;
            this.ResizeEnd += Form1_ResizeEnd;

            lblStatusOK.Text = "";
            lblStatusNotOK.Text = "";
            _ftpHost = ConfigurationManager.AppSettings["ftpHost"];
            _ftpUser = ConfigurationManager.AppSettings["ftpUser"];
            _ftpPass = ConfigurationManager.AppSettings["ftpPass"];
            _ftpExpRoot = ConfigurationManager.AppSettings["ftpExpRoot"];
            _ftpImpRoot = ConfigurationManager.AppSettings["ftpImpRoot"];
            _dbConnection = ConfigurationManager.AppSettings["DbConnection"];
            _dbEdidata1 = ConfigurationManager.AppSettings["DbEdidata1"];

            dateTimePicker1.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = "MM/yyyy";
            dateTimePicker1.ShowUpDown = true;

            initDataGridViewColumns(dataGridView1);
        }

        private void ToggleAntiFlicker(bool Enable)
        {
            bEnableAntiFlicker = Enable;
            this.MaximizeBox = true;
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            ToggleAntiFlicker(false);
        }

        private void Form1_ResizeBegin(object sender, EventArgs e)
        {
            ToggleAntiFlicker(true);
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
        }

        private void initDataGridViewColumns(DataGridView dgv)
        {
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersHeight = 28;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Tahoma", 8.25f, FontStyle.Regular);
            dgv.AutoGenerateColumns = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.RowTemplate.Height = 24;
            dgv.RowTemplate.MinimumHeight = 22;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToResizeRows = false;

            dgv.Columns.Add(createDataGridViewTextBoxColumn("Branch", "Branch", 100, DataGridViewContentAlignment.MiddleCenter));

            dgv.Columns.Add(createDataGridViewTextBoxColumn("DecNo", "DecNo", 150, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("Invoice No.", "InvNO", 150, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("CompCd", "CompCd", 80, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("Div", "DivCd", 80, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("Trad", "TransportType", 80, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("RawCode", "RawCode", 80, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("Status", "Status", 100, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("Location", "Location", 300, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("Download", "DownloadStatus", 150, DataGridViewContentAlignment.MiddleCenter));
        }

        private DataGridViewTextBoxColumn createDataGridViewTextBoxColumn(
           string title, string propertyName, int width,
           DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
        {
            var col = new DataGridViewTextBoxColumn
            {
                DataPropertyName = propertyName,
                HeaderText = title.ToUpper(),
                Name = "__col__" + propertyName,
                ReadOnly = true,
                Width = width
            };

            col.DefaultCellStyle.Alignment = alignment;
            return col;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (cboShipmentType.Text == "")
            {
                MessageBox.Show("Please select shipment type.");
                return;
            }

            dataGridView1.DataSource = null;
            backgroundWorker1.RunWorkerAsync(new object[] { dateTimePicker1.Value, cboShipmentType.Text });
            btnSearch.Enabled = false;
            progressBar1.Style = ProgressBarStyle.Marquee;
            lblStatusOK.Text = "";
            lblStatusNotOK.Text = "";
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var args = e.Argument as object[];
                var monthSelected = (DateTime)args[0];
                var shipmentType = (string)args[1];
                var invController = new InvoiceController(_dbConnection, _dbEdidata1);
                var tranController = new TransactionController(_dbConnection, _dbEdidata1);

                var cultureInfo = new System.Globalization.CultureInfo("en-US");
                var lastDays = new DateTime(monthSelected.Year, monthSelected.Month, DateTime.DaysInMonth(monthSelected.Year, monthSelected.Month));
                var fromDate = monthSelected.ToString("yyyyMMdd", cultureInfo);
                var toDate = lastDays.ToString("yyyyMMdd", cultureInfo);
                List<Invoice> invoices = new List<Invoice>();
                List<Shipment> shipments = new List<Shipment>();

                // Load Commercial invoice.
                if (shipmentType.ToUpper() == "EXPORT")
                {
                    invoices = invController.ExportShipmentResult(fromDate, toDate);
                    shipments = tranController.ExportShipmentResult(fromDate, toDate);
                }
                else
                {
                    invoices = invController.ImportShipmentResult(fromDate, toDate);
                    shipments = tranController.ImportShipmentResult(fromDate, toDate);
                }

                for (int i = 0; i < invoices.Count; i++)
                {
                    var shipment = shipments.Where(q => q.DecNO.TrimEnd() == invoices.ElementAt(i).DecNo.TrimEnd());

                    if (shipment == null || shipment.Count() == 0)
                        invoices.ElementAt(i).Status = "I";
                    else
                    {
                        invoices.ElementAt(i).Status = shipment.ElementAt(0).Status;
                        invoices.ElementAt(i).RawCode = shipment.ElementAt(0).MaterialType;
                        invoices.ElementAt(i).Branch = shipment.ElementAt(0).BranchSeq;

                        if (shipment.ElementAt(0).Status == "A")
                            invoices.ElementAt(i).Location = string.Format("{0}/{1}.pdf",
                                shipmentType.ToUpper() == "EXPORT" ? _ftpExpRoot : _ftpImpRoot,
                                invoices.ElementAt(i).DecNo.TrimEnd());
                    }
                }

                invoices.ForEach(q =>
                {
                    if (q.Status == "I")
                    {
                        Shipment chkShipment;

                        try
                        {
                            if (shipmentType.ToUpper() == "IMPORT")
                                chkShipment = tranController.ImportShipmentVerifyResult(q.DecNo.TrimEnd());
                            else
                                chkShipment = tranController.ExportShipmentVerifyResult(q.DecNo.TrimEnd());

                            if (chkShipment != null)
                            {
                                q.RawCode = chkShipment.MaterialType;
                                q.Branch = chkShipment.BranchSeq;
                                q.Status = chkShipment.Status;
                                q.Location = string.Format("{0}/{1}.pdf",
                                shipmentType.ToUpper() == "EXPORT" ? _ftpExpRoot : _ftpImpRoot, q.DecNo.TrimEnd());
                            }
                        }
                        catch { }
                    }
                });

                e.Result = invoices;
            }
            catch { }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnSearch.Enabled = true;
            progressBar1.Style = ProgressBarStyle.Continuous;

            _invoiceLst = e.Result as List<Invoice>;
            dataGridView1.DataSource = _invoiceLst;
            showSummary(_invoiceLst);
        }

        private void chkPendingStatus_Click(object sender, EventArgs e)
        {
            if (_invoiceLst == null || _invoiceLst.Count == 0)
            {
                chkPendingStatus.Checked = false;
                return;
            }

            if (chkPendingStatus.CheckState == CheckState.Checked)
            {
                dataGridView1.DataSource = null;
                var _pendingInv = _invoiceLst.Where(q => q.Status == "I").ToList();
                dataGridView1.DataSource = _pendingInv;
                showSummary(_pendingInv);
            }
            else
            {
                dataGridView1.DataSource = _invoiceLst;
                showSummary(_invoiceLst);
            }
        }

        private void showSummary(List<Invoice> items)
        {
            lblStatusOK.Text = "0";
            lblStatusNotOK.Text = "0";

            try
            {
                var totalOK = items.Count(q => q.Status == "A");
                var totalNotOk = items.Count - totalOK;

                lblStatusOK.Text = string.Format("Found {0} ", totalOK.ToString("#,##0"));
                lblStatusNotOK.Text = string.Format("Not Found {0}", totalNotOk.ToString("#,##0"));
            }
            catch { }
        }

        private void chkNotFoundRawMat_Click(object sender, EventArgs e)
        {
            if (_invoiceLst == null || _invoiceLst.Count == 0)
            {
                return;
            }

            if (chkNotFoundRawMat.CheckState == CheckState.Checked)
            {
                var items = _invoiceLst.Where(q => q.RawCode == "").ToList();
                dataGridView1.DataSource = items;
            }
            else
            {
                dataGridView1.DataSource = _invoiceLst;
            }
        }

        private void btnVerify_Click(object sender, EventArgs e)
        {
            backgroundWorker2.RunWorkerAsync();

            dataGridView1.DataSource = _invoiceLst;
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var ftpController = new FtpController(_ftpHost, _ftpUser, _ftpPass);

                for (int i = 0; i < _invoiceLst.Count; i++)
                {
                    var inv = _invoiceLst[i];
                    inv.DownloadStatus = ftpController.FileExist(inv.Location).ToString();
                }

                e.Result = true;
            }
            catch { }
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            dataGridView1.DataSource = _invoiceLst;
        }
    }
}