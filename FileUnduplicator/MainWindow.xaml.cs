using FileUnduplicator.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Security.Cryptography;
using System.Security;

namespace FileUnduplicator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FolderCollectionViewModel foldersViewModel;
        public MainWindow()
        {
            InitializeComponent();
            foldersViewModel = new FolderCollectionViewModel();
            this.DataContext = foldersViewModel;
        }

        private void ButtonAddFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Folder folder = new Folder();
                    folder.FolderPath = dialog.SelectedPath;
                    folder.ID = foldersViewModel.GetNextUploadFolderID();
                    foldersViewModel.Folders.Add(folder);
                }
            }
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            int folderId = Convert.ToInt32(((Button)sender).Tag);
            var folder = foldersViewModel.Folders.Where(f => f.ID == folderId).Single();

            foldersViewModel.Folders.Remove(folder);
        }

        private void ButtonSetDestinationFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    foldersViewModel.DestinationFolderPath = dialog.SelectedPath;
                }
            }
        }

        private async void ButtonRun_Click(object sender, RoutedEventArgs e)
        {
            if (foldersViewModel.Folders.Count == 0)
            {
                MessageBox.Show("Please select at least one source folder.");
            }
            else if (string.IsNullOrWhiteSpace(foldersViewModel.DestinationFolderPath))
            {
                MessageBox.Show("Please select a destination folder.");
            }
            else
            {
                await Task.Run(() =>
                {
                    DirectoryInfo destinationFolder = new DirectoryInfo(foldersViewModel.DestinationFolderPath);

                    try
                    {
                        destinationFolder.EnumerateFiles();
                    }
                    catch (SecurityException)
                    {
                        MessageBox.Show($"Access to destination folder was denied! Operation aborted.");
                        return;
                    }

                    List<DirectoryInfo> sourceFolders = new List<DirectoryInfo>();
                    foreach (Folder source in foldersViewModel.Folders)
                    {
                        try
                        {
                            DirectoryInfo sourceFolder = new DirectoryInfo(source.FolderPath);
                            sourceFolders.Add(sourceFolder);
                        }
                        catch (SecurityException)
                        {

                            MessageBox.Show($"Access to {source.FolderPath} was denied!");
                            return;
                        }
                    }
                    CopyDirectories(sourceFolders, destinationFolder);

                    alreadyCopiedFiles = new Dictionary<string, bool>();
                });
            }
        }

        private Dictionary<string, bool> alreadyCopiedFiles = new Dictionary<string, bool>();

        private void CopyDirectories(IEnumerable<DirectoryInfo> directories, DirectoryInfo destination)
        {
            foreach (DirectoryInfo directory in directories)
            {
                IEnumerable<DirectoryInfo> subFolders = null;
                IEnumerable<FileInfo> files = null;
                try
                {
                    subFolders = directory.EnumerateDirectories();
                    files = directory.EnumerateFiles();
                }
                catch (SecurityException)
                {
                    MessageBox.Show($"Access to {directory.FullName} was denied!");
                }

                if (subFolders != null && files != null)
                {
                    CopyDirectories(subFolders, destination);
                    foreach (FileInfo file in files)
                    {
                        using (var md5 = MD5.Create())
                        {
                            using (var stream = file.OpenRead())
                            {
                                string hash = Encoding.Default.GetString(md5.ComputeHash(stream));
                                if (!alreadyCopiedFiles.ContainsKey(hash))
                                {
                                    alreadyCopiedFiles.Add(hash, true);
                                    string newFileName = GetUniqueFileName(file.Name, destination);
                                    File.Copy(file.FullName, System.IO.Path.Combine(destination.FullName, newFileName));
                                }
                            }
                        }
                    }
                }
            }
        }

        private string GetUniqueFileName(string fileName, DirectoryInfo destination)
        {
            var existingFileNames = destination.GetFiles().Select(f => f.Name);

            string uniqueFileName = fileName;
            string fileExtension = System.IO.Path.GetExtension(fileName);
            string fileNameWithoutExtenstion = System.IO.Path.GetFileNameWithoutExtension(fileName);
            int fileNumber = 1;
            while (existingFileNames.Contains(uniqueFileName))
            {
                uniqueFileName = fileNameWithoutExtenstion + $"({fileNumber})" + fileExtension;
                fileNumber += 1;
            }

            return uniqueFileName;
        }
    }
}
