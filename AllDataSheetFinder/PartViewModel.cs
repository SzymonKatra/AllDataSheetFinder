using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVMUtils;

namespace AllDataSheetFinder
{
    public class PartViewModel : ObservableObject, IModelExposable<Part>
    {
        public PartViewModel()
            : this(new Part())
        {
        }
        public PartViewModel(Part model, bool makeContext = true)
        {
            m_model = model;
            MakeContext();
        }

        public static PartViewModel FromAllDataSheetPart(AllDataSheetPart part)
        {
            PartViewModel result = new PartViewModel();
            result.Name = part.Name;
            result.Description = part.Description;
            result.Manufacturer = part.Manufacturer;
            result.ManufacturerImageLink = part.ManufacturerImageLink;
            result.Context = part;
            return result;
        }

        private Part m_model;
        public Part Model
        {
            get { return m_model; }
        }

        public string Name
        {
            get { return m_model.Name; }
            set { m_model.Name = value; RaisePropertyChanged("Name"); RaisePropertyChanged("Code"); }
        }
        public string Description
        {
            get { return m_model.Description; }
            set { m_model.Description = value; RaisePropertyChanged("Description"); }
        }
        public string Manufacturer
        {
            get { return m_model.Manufacturer; }
            set { m_model.Manufacturer = value; RaisePropertyChanged("Manufacturer"); RaisePropertyChanged("Code"); }
        }
        public string ManufacturerImageLink
        {
            get { return m_model.ManufacturerImageLink; }
            set { m_model.ManufacturerImageLink = value; RaisePropertyChanged("ManufacturerImageLink"); }
        }
        public string DatasheetSiteLink
        {
            get { return m_model.DatasheetSiteLink; }
            set { m_model.DatasheetSiteLink = value; RaisePropertyChanged("DatasheetSiteLink"); RaisePropertyChanged("Code"); }
        }
        public string DatasheetPdfLink
        {
            get { return m_model.DatasheetPdfLink; }
            set { m_model.DatasheetPdfLink = value; RaisePropertyChanged("DatasheetPdfLink"); }
        }
        public long DatasheetSize
        {
            get { return m_model.DatasheetSize; }
            set { m_model.DatasheetSize = value; RaisePropertyChanged("DatasheetSize"); }
        }
        public int DatasheetPages
        {
            get { return m_model.DatasheetPages; }
            set { m_model.DatasheetPages = value; RaisePropertyChanged("DatasheetPages"); }
        }
        public string ManufacturerSite
        {
            get { return m_model.ManufacturerSite; }
            set { m_model.ManufacturerSite = value; RaisePropertyChanged("ManufacturerSite"); }
        }
        public DateTime LastUseDate
        {
            get { return m_model.LastUseDate; }
            set { m_model.LastUseDate = value; RaisePropertyChanged("LastUseDate"); }
        }
        public bool Custom
        {
            get { return m_model.Custom; }
            set { m_model.Custom = value; RaisePropertyChanged("Custom"); }
        }
        public string CustomPath
        {
            get { return m_model.CustomPath; }
            set { m_model.CustomPath = value; RaisePropertyChanged("CustomPath"); }
        }

        public string Code
        {
            get
            {
                return BuildCodeFromLink(DatasheetSiteLink, Name, Manufacturer, DatasheetSiteLink.GetHashCode().ToString());
            }
        }

        private AllDataSheetPart m_context;
        protected AllDataSheetPart Context
        {
            get { return m_context; }
            set { m_context = value; }
        }

        public bool IsContextValid
        {
            get { return m_context != null; }
        }

        public void MakeContext()
        {
            m_context = new AllDataSheetPart(DatasheetSiteLink);
            RaisePropertyChanged("IsContextValid");
        }

        public static string BuildCodeFromLink(string link, string name, string manufacturer, string hash)
        {
            Uri uri = new Uri(link);
            string[] segments = uri.Segments;
            if (segments.Length < 3) return BuildCode(name, manufacturer, link.GetHashCode().ToString());
            string nameSegment = segments[segments.Length - 1];
            string manufacturerSegment = segments[segments.Length - 2];
            string numberSegment = segments[segments.Length - 3];

            nameSegment = nameSegment.Remove(nameSegment.Length - 5); // remove ".html"
            manufacturerSegment = manufacturerSegment.Remove(manufacturerSegment.Length - 1); // remove slash
            numberSegment = numberSegment.Remove(numberSegment.Length - 1); // remove slash

            return BuildCode(nameSegment, manufacturerSegment, numberSegment);
        }
        public static string BuildCode(string name, string manufacturer, string hash)
        {
            name = ToValidCodeForm(name);
            manufacturer = ToValidCodeForm(manufacturer);
            hash = ToValidCodeForm(hash);

            return string.Format("{0}_{1}_{2}", name, manufacturer, hash);
        }

        private static string ToValidCodeForm(string value)
        {
            return value.Replace(' ', '-').RemoveAll(x => !char.IsLetterOrDigit(x));
        }
    }
}
