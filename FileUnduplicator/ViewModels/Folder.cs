using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUnduplicator.ViewModels
{
    public class Folder : BaseViewModel
    {
        public int ID { get; set; }
        public string FolderPath { get; set; }
    }
}
