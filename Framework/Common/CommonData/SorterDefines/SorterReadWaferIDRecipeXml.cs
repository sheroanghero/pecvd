using Aitex.Core.Common;
using Aitex.Core.RT.Log;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using MECF.Framework.Common.Equipment;
using System.IO;

namespace Aitex.Sorter.Common
{
    [DataContract]
    [Serializable]
    public class SorterReadWaferIDRecipeXml : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string name;
        [DataMember]
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        private bool _isReadLaserMarker1 = false;
        [DataMember]
        public bool IsReadLaserMarker1
        {
            get { return _isReadLaserMarker1; }
            set
            {
                _isReadLaserMarker1 = value;
                OnPropertyChanged("IsReadLaserMarker1");
            }
        }

        private bool _isVerifyLaserMarker1;
        [DataMember]
        public bool IsVerifyLaserMarker1
        {
            get { return _isVerifyLaserMarker1; }
            set
            {
                _isVerifyLaserMarker1 = value;
                OnPropertyChanged("IsVerifyLaserMarker1");
            }
        }

        private bool _isVerifyChecksumLaserMarker1;
        [DataMember]
        public bool IsVerifyChecksumLaserMarker1
        {
            get { return _isVerifyChecksumLaserMarker1; }
            set
            {
                _isVerifyChecksumLaserMarker1 = value;
                OnPropertyChanged("IsVerifyChecksumLaserMarker1");
            }
        }

        private bool _isReadLaserMarker2 = false;
        [DataMember]
        public bool IsReadLaserMarker2
        {
            get { return _isReadLaserMarker2; }
            set
            {
                _isReadLaserMarker2 = value;
                OnPropertyChanged("IsReadLaserMarker2");
            }
        }

        private bool _isVerifyLaserMarker2;
        [DataMember]
        public bool IsVerifyLaserMarker2
        {
            get { return _isVerifyLaserMarker2; }
            set
            {
                _isVerifyLaserMarker2 = value;
                OnPropertyChanged("IsVerifyLaserMarker2");
            }
        }

        private bool _isVerifyChecksumLaserMarker2;
        [DataMember]
        public bool IsVerifyChecksumLaserMarker2
        {
            get { return _isVerifyChecksumLaserMarker2; }
            set
            {
                _isVerifyChecksumLaserMarker2 = value;
                OnPropertyChanged("IsVerifyChecksumLaserMarker2");
            }
        }

        private double _preAlignAngle = 0;
        [DataMember]
        public double PreAlignAngle
        {
            get { return _preAlignAngle; }
            set
            {
                _preAlignAngle = value;
                //OnPropertyChanged("PreAlignAngle");
            }
        }

        private double _postAlignAngle = 0;
        [DataMember]
        public double PostAlignAngle
        {
            get { return _postAlignAngle; }
            set
            {
                _postAlignAngle = value;
                OnPropertyChanged("PostAlignAngle");
            }
        }

        private bool _isPostAlign;
        [DataMember]
        public bool IsPostAlign
        {
            get { return _isPostAlign; }
            set
            {
                _isPostAlign = value;
                OnPropertyChanged("IsPostAlign");
            }
        }        
        
        private bool _isTurnOver;
        [DataMember]
        public bool IsTurnOver
        {
            get { return _isTurnOver; }
            set
            {
                _isTurnOver= value;
                OnPropertyChanged("IsTurnOver");
            }
        }   

        private int _laserMarkerWaferReaderIndex1;
        [DataMember]
        public int LaserMarkerWaferReaderIndex1
        {
            get => _laserMarkerWaferReaderIndex1;
            set
            {
                _laserMarkerWaferReaderIndex1 = value;
                OnPropertyChanged("LaserMarkerWaferReaderIndex1");
            }
        }

        private int _laserMarkerWaferReaderIndex2;
        [DataMember]
        public int LaserMarkerWaferReaderIndex2
        {
            get => _laserMarkerWaferReaderIndex2;
            set
            {
                _laserMarkerWaferReaderIndex2 = value;
                OnPropertyChanged("LaserMarkerWaferReaderIndex2");
            }
        }

        private ObservableCollection<KeyValuePair<string, string>> _ocrReaderJob1;
        [DataMember]
        public ObservableCollection<KeyValuePair<string, string>> OcrReaderJob1
        {
            get => _ocrReaderJob1;
            set
            {
                _ocrReaderJob1 = value;
                OnPropertyChanged("OcrReaderJob1");
            }
        }

        private ObservableCollection<KeyValuePair<string, string>> _ocrReaderJob2;
        [DataMember]
        public ObservableCollection<KeyValuePair<string, string>> OcrReaderJob2
        {
            get => _ocrReaderJob2;
            set
            {
                _ocrReaderJob2 = value;
                OnPropertyChanged("OcrReaderJob2");
            }
        }
        public SorterReadWaferIDRecipeXml(string name = "")
        {
            Name = name;
            OcrReaderJob1 = new ObservableCollection<KeyValuePair<string, string>>();
            OcrReaderJob2 = new ObservableCollection<KeyValuePair<string, string>>();
        }

        public SorterReadWaferIDRecipeXml(string content, string name = "")
        {
            Name = name;
            SetContent(content);
        }

        public bool SetContent(string content)
        {
            try
            {
                var doc = XDocument.Parse(content);
                ParseContent(doc);
            }
            catch (Exception e)
            {
                LOG.Write(e);
                return false;
            }

            return true;
        }

        public string GetContent()
        {
            var doc = new XDocument();
            var root = new XElement("Recipe");
            var element = new XElement("AitexSorterReadWaferIDRecipe"
                , new XAttribute("PreAlignAngle", PreAlignAngle)
                , new XAttribute("IsReadLaserMarker1", IsReadLaserMarker1.ToString())
                , new XAttribute("IsVerifyLaserMarker1", IsVerifyLaserMarker1.ToString())
                , new XAttribute("IsVerifyChecksumLaserMarker1", IsVerifyChecksumLaserMarker1.ToString())
                , new XAttribute("LaserMarkerWaferReaderIndex1", LaserMarkerWaferReaderIndex1)
                , new XAttribute("IsReadLaserMarker2", IsReadLaserMarker2.ToString())
                , new XAttribute("IsVerifyLaserMarker2", IsVerifyLaserMarker2.ToString())
                , new XAttribute("IsVerifyChecksumLaserMarker2", IsVerifyChecksumLaserMarker2.ToString())
                , new XAttribute("LaserMarkerWaferReaderIndex2", LaserMarkerWaferReaderIndex2)
                , new XAttribute("IsPostAlign", IsPostAlign.ToString())
                , new XAttribute("PostAlignAngle", PostAlignAngle)
                , new XAttribute("OcrReaderJob1", OcrReaderJob1 != null ? string.Join(";", OcrReaderJob1.Select(x => x.Value)) : "")
                , new XAttribute("OcrReaderJob2", OcrReaderJob2 != null ? string.Join(";", OcrReaderJob2.Select(x => x.Value)) : "")
                , new XAttribute("IsTurnOver", IsTurnOver.ToString())
                );
            root.Add(element);
            doc.Add(root);

            return doc.ToString();
        }
        private void ParseContent(XDocument doc)
        {
            try
            {
                var node = doc.Root.Element("AitexSorterReadWaferIDRecipe");
                if (node == null)
                {
                    LOG.Write(string.Format("recipe not valid"));
                    return;
                }

                string value = node.Attribute("PreAlignAngle").Value;
                double preAngle;
                double.TryParse(value, out preAngle);
                PreAlignAngle = preAngle;

                value = node.Attribute("IsReadLaserMarker1").Value;
                bool isReadLaserMarker1;
                bool.TryParse(value, out isReadLaserMarker1);
                IsReadLaserMarker1 = isReadLaserMarker1;

                value = node.Attribute("IsVerifyLaserMarker1").Value;
                bool isVerifyLaserMarker1;
                bool.TryParse(value, out isVerifyLaserMarker1);
                IsVerifyLaserMarker1 = isVerifyLaserMarker1;

                value = node.Attribute("IsVerifyChecksumLaserMarker1").Value;
                bool isVerifyChecksumLaserMarker1;
                bool.TryParse(value, out isVerifyChecksumLaserMarker1);
                IsVerifyChecksumLaserMarker1 = isVerifyChecksumLaserMarker1;

                LaserMarkerWaferReaderIndex1 = GetValue<int>(node, "LaserMarkerWaferReaderIndex1");

                value = node.Attribute("IsReadLaserMarker2").Value;
                bool isReadLaserMarker2;
                bool.TryParse(value, out isReadLaserMarker2);
                IsReadLaserMarker2 = isReadLaserMarker2;

                value = node.Attribute("IsVerifyLaserMarker2").Value;
                bool isVerifyLaserMarker2;
                bool.TryParse(value, out isVerifyLaserMarker2);
                IsVerifyLaserMarker2 = isVerifyLaserMarker2;

                value = node.Attribute("IsVerifyChecksumLaserMarker2").Value;
                bool isVerifyChecksumLaserMarker2;
                bool.TryParse(value, out isVerifyChecksumLaserMarker2);
                IsVerifyChecksumLaserMarker2 = isVerifyChecksumLaserMarker2;

                LaserMarkerWaferReaderIndex2 = GetValue<int>(node, "LaserMarkerWaferReaderIndex2");

                value = node.Attribute("IsPostAlign").Value;
                bool ispostAlign;
                bool.TryParse(value, out ispostAlign);
                IsPostAlign = ispostAlign;

                value = node.Attribute("PostAlignAngle").Value;
                double postAngle;
                double.TryParse(value, out postAngle);
                PostAlignAngle = postAngle;

                value = node.Attribute("OcrReaderJob1").Value;
                var arr = value.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
                OcrReaderJob1 = new ObservableCollection<KeyValuePair<string, string>>(arr.ToDictionary(k => Path.GetFileNameWithoutExtension(k), v => v));

                value = node.Attribute("OcrReaderJob2").Value;
                arr = value.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
                OcrReaderJob2 = new ObservableCollection<KeyValuePair<string, string>>(arr.ToDictionary(k => Path.GetFileNameWithoutExtension(k), v => v));
                
                value = node.Attribute("IsTurnOver").Value;
                bool isTurnOver;
                bool.TryParse(value, out isTurnOver);
                IsTurnOver = isTurnOver;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }
        private T GetValue<T>(XElement element, string Name)
        {
            var attr = element.Attribute(Name);
            if (attr != null)
            {
                return (T)Convert.ChangeType(attr.Value, typeof(T));
            }
            return default(T);
        }
    }
}