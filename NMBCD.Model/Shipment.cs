using System;

namespace NMBCD.Model
{
    public class Shipment
    {
        public string BranchSeq { get; set; }
        public string DatabaseSeq { get; set; }
        public string TransportMode { get; set; }
        public string BranchCode { get; set; }
        public string RefNO { get; set; }
        public string DecNO { get; set; }
        public string DocStatus { get; set; }
        public string TaxNo { get; set; }
        public string MasterBL { get; set; }
        public string HouseBL { get; set; }
        public string RefDecNO { get; set; }
        public string Status { get; set; }
        public string ShipmentType { get; set; }
        public string MaterialType { get; set; }
        public DateTime CreateDate { get; set; }

        public string ButtonText
        {
            get { return "Download"; }
        }
    }
}