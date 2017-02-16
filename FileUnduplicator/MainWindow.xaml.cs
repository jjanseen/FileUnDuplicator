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
                totalFiles = 0;
                alreadyCopiedFiles = new Dictionary<string, bool>();
                DisableControls();
                ProgressBar.Visibility = Visibility.Visible;

                try
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

                    await SetTotalFileCount(sourceFolders);
                    await CopyDirectories(sourceFolders, destinationFolder, 0);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was an error: " + ex.Message);
                }
                finally
                {
                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ProgressBar.Visibility = Visibility.Collapsed;
                    }));
                    EnableControls();
                }
            }
        }

        private void DisableControls()
        {
            MainGrid.IsEnabled = false;
        }

        private void EnableControls()
        {
            MainGrid.IsEnabled = true;
        }

        private async Task SetTotalFileCount(IEnumerable<DirectoryInfo> directories)
        {
            foreach (DirectoryInfo directory in directories)
            {
                IEnumerable<DirectoryInfo> subFolders = null;
                IEnumerable<FileInfo> files = null;
                try
                {
                    await Task.Run(() =>
                    {
                        subFolders = directory.EnumerateDirectories();
                        files = directory.EnumerateFiles();
                    });
                }
                catch (SecurityException)
                {
                    MessageBox.Show($"Access to {directory.FullName} was denied!");
                }
                if (subFolders != null && files != null)
                {
                    totalFiles += files.Count();
                    await SetTotalFileCount(subFolders);
                }
            }
        }

        private double totalFiles = 0;
        private Dictionary<string, bool> alreadyCopiedFiles = new Dictionary<string, bool>();

        private async Task CopyDirectories(IEnumerable<DirectoryInfo> directories, DirectoryInfo destination, int processedFiles)
        {
            int filesProcessed = processedFiles;
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
                    await CopyDirectories(subFolders, destination, filesProcessed);
                    foreach (FileInfo file in files)
                    {
                        await Task.Run(() =>
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
                        });
                        filesProcessed += 1;
                        await UpdateProgress(filesProcessed);
                    }
                }
            }
        }

        private async Task UpdateProgress(int processedFiles)
        {
            await Dispatcher.BeginInvoke(new System.Action(() =>
            {
                ProgressBar.ProgressBarStatus.Value = ((double)processedFiles / totalFiles) * 100;
            }));
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
