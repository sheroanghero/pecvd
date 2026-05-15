using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeEditorLib.RecipeModel.Params
{
    public class PathFileParam : Param
    {
        //value is full path name
        private string _value;
        public string Value
        {
            get { return this._value; }
            set
            {
                this._value = value;
                if (this.Feedback != null)
                {
                    this.Feedback(this);
                }

                if (string.IsNullOrEmpty(_value))
                {
                    FileName = "";
                }
                else
                {
                    int index = _value.LastIndexOf("\\");
                    if (index > -1)
                    {
                        FileName = _value.Substring(index + 1);
                    }
                    else
                    {
                        FileName = _value;
                    }
                }
                


                this.NotifyOfPropertyChange("Value");
            }
        }

        private string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                NotifyOfPropertyChange("FileName");
            }
        }

        private string _prefixPath;
        public string PrefixPath
        {
            get { return _prefixPath; }
            set
            {
                _prefixPath = value;
                NotifyOfPropertyChange("PrefixPath");
            }
        }
    }
}
