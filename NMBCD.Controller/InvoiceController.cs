using ClosedXML.Excel;
using Newtonsoft.Json;
using NMBCD.Model;
using RestSharp;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace NMBCD.Controller
{
    public class InvoiceController
    {
        private readonly string _connectionString;
        private readonly string _connectionForEdi;

        public InvoiceController(string connectionString, string connectionForEdi)
        {
            _connectionString = connectionString;
            _connectionForEdi = connectionForEdi;
        }

        private DataSet getDataset(string shipmentType, string fromdate, string todate)
        {
            DataSet dsResult = null;

            try
            {
                var conn = new SqlConnection(_connectionForEdi);
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 0;

                if (shipmentType == "EXP")
                    cmd.CommandText = @"select DecNo, InvNo, CompCd,
                (select top 1 IHDEPT from HD010 where IHINVN=InvNo) as Div,
                TradCd from nmb02 where clrdate>=@fromDate and clrdate <= @toDate";
                else
                {
                    cmd.CommandText = @"select EntryNo As DecNo, InvNo, ImportCD as CompCd, ImportDV as Div,
                (select top 1 H5Trad from HD050 where H5INVN=InvNo) as TradCd
                from nmb05 where ClearDate >= @fromDate and ClearDate <= @toDate";
                }

                cmd.Parameters.AddWithValue("@fromDate", fromdate);
                cmd.Parameters.AddWithValue("@toDate", todate);

                var adapter = new SqlDataAdapter(cmd);
                var ds = new DataSet();
                adapter.Fill(ds);

                conn.Close();

                // Check Status.
                var dt = ds.Tables[0].Clone();

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    if (!IsCancelDeclaration(dr["DecNo"].ToString(), shipmentType))
                    {
                        dt.Rows.Add(dr.ItemArray);
                    }
                }

                dt.AcceptChanges();
                dsResult = new DataSet();
                dsResult.Tables.Add(dt);
                dsResult.AcceptChanges();
            }
            catch { }

            return dsResult;
        }

        private bool IsCancelDeclaration(string decno, string shipmentType)
        {
            var result = false;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();

                    if (shipmentType == "EXP")
                        cmd.CommandText = @"select 1 from DecX_Declare where DecNo=@decno and DocStatus >= 98";
                    else
                        cmd.CommandText = @"select 1 from DecI_Declare where DecNo=@decno and DocStatus >= 98";

                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@decno", decno);

                    var objReader = cmd.ExecuteScalar();

                    if (objReader != null && objReader.ToString() == "1")
                    {
                        result = true;
                    }
                }
            }
            catch { }

            return result;
        }

        public List<Invoice> ExportShipmentResult(string fromDate, string toDate)
        {
            DataSet dsExport = getDataset("EXP", fromDate, toDate);

            var invoices = new List<Invoice>();
            foreach (DataRow dr in dsExport.Tables[0].Rows)
            {
                invoices.Add(ConvertDataRowToNmbInvoice(dr));
            }

            return invoices;
        }

        public List<Invoice> ImportShipmentResult(string fromDate, string toDate)
        {
            DataSet dsExport = getDataset("IMP", fromDate, toDate);

            var invoices = new List<Invoice>();
            foreach (DataRow dr in dsExport.Tables[0].Rows)
            {
                invoices.Add(ConvertDataRowToNmbInvoice(dr));
            }

            return invoices;
        }

        private Invoice ConvertDataRowToNmbInvoice(DataRow row)
        {
            return new Invoice()
            {
                DecNo = row["DecNo"].ToString(),
                InvNO = row["InvNo"].ToString(),
                CompCd = row["CompCd"].ToString(),
                DivCd = row["Div"].ToString(),
                TransportType = row["TradCd"].ToString(),
                Status = ""
            };
        }

        //public List<Invoice> ImportShipmentResult(string fromDate, string toDate)
        //{
        //    DataSet dsResult = new DataSet();

        //    using (SqlConnection conn = new SqlConnection("data source=198.1.1.3;uid=sa;password=zy01sy?;database=EDIDATA1"))
        //    {
        //        conn.Open();
        //        var cmd = conn.CreateCommand();
        //        cmd.CommandType = CommandType.Text;
        //        cmd.CommandText = @"select EntryNo As DecNo, InvNo, ImportCD as CompCd, ImportDV as Div,
        //        (select top 1 H5Trad from HD050 where H5INVN=InvNo) as TradCd
        //        from nmb05 where ClearDate >= @fromDate and ClearDate <= @toDate";

        //        cmd.Parameters.AddWithValue("@fromdate", fromDate);
        //        cmd.Parameters.AddWithValue("@todate", toDate);

        //        var da = new SqlDataAdapter(cmd);
        //        da.Fill(dsResult);
        //        conn.Close();
        //    }

        //    var inv = new List<Invoice>();

        //    foreach (DataRow dr in dsResult.Tables[0].Rows)
        //    {
        //        inv.Add(new Invoice
        //        {
        //            InvNO = dr["InvNO"].ToString(),
        //            DecNo = dr["DecNo"].ToString(),
        //            CompCd = dr["CompCd"].ToString(),
        //            DivCd = dr["Div"].ToString(),
        //            TransportType = dr["TradCd"].ToString()
        //        });
        //    }

        //    return inv;
        //}

        public void WriteExcel(List<Invoice> data, string name, string location)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet(name);

            var range = worksheet.Range(worksheet.Cell(1, 1), worksheet.Cell(1, 4));
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Font.Bold = true;
            range.Style.Font.FontName = "Tahoma";
            range.Style.Font.FontSize = 10;

            worksheet.Cell(1, 1).Value = "BRANCH";
            worksheet.Cell(1, 2).Value = "DECNO";
            worksheet.Cell(1, 3).Value = "INVOICE";
            worksheet.Cell(1, 4).Value = "STATUS";

            for (int i = 1; i < 4; i++)
            {
                worksheet.Column(i).Width = 15;
                worksheet.Column(i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Column(i).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            int iRow = 2;

            for (int i = 0; i < data.Count; i++)
            {
                var invoice = data.ElementAt(i);
                worksheet.Cell(iRow, 1).Value = invoice.Branch;
                worksheet.Cell(iRow, 2).Value = invoice.DecNo;
                worksheet.Cell(iRow, 3).Value = invoice.InvNO;
                worksheet.Cell(iRow, 4).Value = invoice.Status;

                iRow++;
            }

            var destinationFile = Path.Combine(location, name + ".xlsx");
            if (File.Exists(destinationFile))
                File.Delete(destinationFile);

            workbook.SaveAs(destinationFile);
        }
    }
}