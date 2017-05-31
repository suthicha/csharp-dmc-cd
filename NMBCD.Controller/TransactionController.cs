using Newtonsoft.Json;
using NMBCD.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace NMBCD.Controller
{
    public class TransactionController
    {
        private readonly string _connectionString;
        private readonly string _connectionForEdi;

        public TransactionController(string connectionString, string connectionForEdi)
        {
            _connectionString = connectionString;
            _connectionForEdi = connectionForEdi;
        }

        public List<Shipment> ExportShipmentResult(string fromDate, string toDate)
        {
            return getShipment("sp_decx_verify", fromDate, toDate);
        }

        public Shipment ExportShipmentVerifyResult(string decno)
        {
            return VerifyShipment("sp_decx_verify_bydecno", decno);
        }

        public List<Shipment> ImportShipmentResult(string fromDate, string toDate)
        {
            return getShipment("sp_deci_verify", fromDate, toDate);
        }

        public Shipment ImportShipmentVerifyResult(string decno)
        {
            return VerifyShipment("sp_deci_verify_bydecno", decno);
        }

        private List<Shipment> getShipment(string commandText, string fromDate, string toDate)
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@FromDate", fromDate);
            cmd.Parameters.AddWithValue("@ToDate", toDate);

            var da = new SqlDataAdapter(cmd);
            var ds = new DataSet();
            da.Fill(ds);

            var shipments = new List<Shipment>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                var dr = ds.Tables[0].Rows[i];
                shipments.Add(ConvertToShipment(dr));
            }

            return shipments;
        }

        private Shipment ConvertToShipment(DataRow row)
        {
            return new Shipment()
            {
                BranchSeq = row["BranchSeq"].ToString(),
                DatabaseSeq = row["DatabaseSeq"].ToString(),
                TransportMode = row["TransportMode"].ToString(),
                BranchCode = row["BranchCode"].ToString(),
                RefNO = row["RefNO"].ToString(),
                DecNO = row["DecNO"].ToString(),
                DocStatus = row["DocStatus"].ToString(),
                TaxNo = row["ExporterTaxNo"].ToString(),
                MasterBL = row["MasterBL"].ToString(),
                HouseBL = row["HouseBL"].ToString(),
                RefDecNO = row["RefDecNO"].ToString(),
                ShipmentType = row["ShipmentType"].ToString(),
                MaterialType = row["MaterialType"].ToString(),
                CreateDate = Convert.ToDateTime(row["CreateDate"]),
                Status = string.IsNullOrEmpty(row["RefDecNO"].ToString()) ? "I" : "A"
            };
        }

        private Shipment VerifyShipment(string commandText, string decno)
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@decno", decno);

            var da = new SqlDataAdapter(cmd);
            var ds = new DataSet();
            da.Fill(ds);

            if (ds == null || ds.Tables[0].Rows.Count == 0)
                return null;
            else
                return ConvertToShipment(ds.Tables[0].Rows[0]);
        }
    }
}