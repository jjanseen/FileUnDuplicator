using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUnduplicator.ViewModels
{
    public class FolderCollectionViewModel : BaseViewModel
    {
        public FolderCollectionViewModel()
        {
            Folders = new ObservableCollection<Folder>();
        }

        private string _destinationFolderPath;
        public string DestinationFolderPath
        {
            get
            {
                return _destinationFolderPath;
            }
            set
            {
                if (_destinationFolderPath != value)
                {
                    _destinationFolderPath = value;
                    OnPropertyChanged("DestinationFolderPath");
                }
            }
        }

        private ObservableCollection<Folder> _folders;
        public ObservableCollection<Folder> Folders
        {
            get
            {
                return _folders;
            }
            set
            {
                if (_folders != value)
                {
                    _folders = value;
                    OnPropertyChanged("Folders");
                }
            }
        }

        public int GetNextUploadFolderID()
        {
            if (Folders.Count == 0)
            {
                return 1;
            }
            else
            {
                return Folders.Max(f => f.ID) + 1;
            }
        }
    }
}
