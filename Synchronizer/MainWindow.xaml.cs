using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Synchronizer
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string Source;
        private static string Destination;
        private static List<string> SourceFiles = new List<string>();
        private static List<string> DestionationFiles = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            Source = string.Empty;
            Destination = string.Empty;
        }

        private void btnSource_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Source = dlg.SelectedPath;
                    btnSource.Content = dlg.SelectedPath;
                }
            }
        }

        private void btnDestination_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Destination = dlg.SelectedPath;
                    btnDestination.Content = dlg.SelectedPath;
                }
            }
        }


        private void btnSyncronize_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Source) && !string.IsNullOrEmpty(Destination))
            {
                var t = new Thread(Compare) {IsBackground = true, Priority = ThreadPriority.Lowest};
                t.Start();
            }
        }

        private void SetPrgTxtStatus(string msg, double percent)
        {
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background,
                new Action(delegate
                {
                    txtStatus.Text = msg;
                    prgBar.Value = percent;
                }));
        }

        private void SetProgBar(double x)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(delegate { prgBar.Value = x; }));
        }

        private void SetbtnSync(bool x)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(delegate { btnSyncronize.IsEnabled = x; }));
        }

        private void SetTxTStatus(string msg)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(delegate { txtStatus.Text = msg; }));
        }

        private void Compare()
        {
            SetbtnSync(false);
            try
            {
                SourceFiles = new List<string>();
                DestionationFiles = new List<string>();

                long cFiles = 0;

                SourceFiles.AddRange(Directory.GetFiles(Source));
                DestionationFiles.AddRange(Directory.GetFiles(Destination));

                foreach (var dir in Directory.GetDirectories(Source, "*.*", SearchOption.AllDirectories))
                {
                    SetTxTStatus("Gathering Info from..." + dir);
                    SourceFiles.AddRange(Directory.GetFiles(dir).ToList());
                }

                foreach (var dir in Directory.GetDirectories(Destination, "*.*", SearchOption.AllDirectories))
                {
                    SetTxTStatus("Gathering Info from..." + dir);
                    DestionationFiles.AddRange(Directory.GetFiles(dir).ToList());
                }


                foreach (var File in SourceFiles)
                {
                    int index;

                    if ((index = DestionationFiles.IndexOf(Destination + File.Replace(Source, string.Empty))) > -1)
                    {
                        SetTxTStatus("Compare... " + File);
                        if (FileCompare(File, DestionationFiles[index]))
                        {
                            SetTxTStatus("Identical Files... " + Path.GetFileName(File));
                        }
                        else
                        {
                            SetTxTStatus("Copy : " + File);
                            System.IO.File.Copy(File, DestionationFiles[index], true);
                        }
                    }
                    else
                    {
                        var destinationFile = Destination + File.Replace(Source, string.Empty);
                        SetTxTStatus("Copy : " + File);
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile) ?? "default");
                        System.IO.File.Copy(File, destinationFile);
                    }

                    SetProgBar(cFiles / (double) SourceFiles.Count * 100);
                    cFiles++;
                }

                SetTxTStatus("Complete...");
                SetProgBar(0);
                SetbtnSync(true);
            }
            catch (Exception ex)
            {
                SetTxTStatus("Error");
                SetbtnSync(true);
                SetProgBar(0);
                MessageBox.Show(ex.ToString());
            }
        }

        private void Compare(string source, string destination)
        {
            //byte result = 0;

            try
            {
                SourceFiles = new List<string>();
                DestionationFiles = new List<string>();

                foreach (var dir in Directory.GetDirectories(source, "*.*", SearchOption.AllDirectories))
                    SourceFiles.AddRange(Directory.GetFiles(dir).ToList());
                foreach (var dir in Directory.GetDirectories(destination, "*.*", SearchOption.AllDirectories))
                    DestionationFiles.AddRange(Directory.GetFiles(dir).ToList());


                foreach (var file in SourceFiles)
                {
                    int index;
                    if ((index = DestionationFiles.IndexOf(destination + file.Replace(source, string.Empty))) > -1)
                    {
                        if (FileCompare(file, DestionationFiles[index]))
                        {
                        }
                        else
                        {
                            System.IO.File.Delete(DestionationFiles[index]);

                            //Synchronizer.MainWindow.SetPrgTxtStatus("Copy: " + File + " To: " + DestionationFiles[Index], 23);
                            //SetPrgTxtStatus("", 23d);
                            //txtStatus.Text = "Copy: " + File + " To: " + DestionationFiles[Index];
                            System.IO.File.Copy(file, DestionationFiles[index]);
                        }
                    }
                    else
                    {
                        var DestinationFile = destination + file.Replace(source, string.Empty);
                        //txtStatus.Text = "Copy: " + File + " To: " + DestinationFile;
                        Directory.CreateDirectory(Path.GetDirectoryName(DestinationFile));
                        System.IO.File.Copy(file, DestinationFile);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private static bool FileCompare(string file1, string file2)
        {
            try
            {
                var fl1 = new FileInfo(file1);
                var fl2 = new FileInfo(file2);

                if (fl1.Length != fl2.Length) return false;


                if (HashFile(file1) != HashFile(file2))
                    return false;
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }

        public static string HashFile(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return HashFile(fs);
            }
        }

        public static string HashFile(FileStream stream)
        {
            var sb = new StringBuilder();

            if (stream != null)
            {
                stream.Seek(0, SeekOrigin.Begin);

                var md5 = MD5.Create();
                var hash = md5.ComputeHash(stream);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));

                stream.Seek(0, SeekOrigin.Begin);
            }

            return sb.ToString();
        }

        private void btnTest_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                Path.GetDirectoryName("");
                Directory.CreateDirectory(@"D:\test\");
                File.WriteAllText(@"D:\test1\t.txt", "23232");
            }
            catch (Exception)
            {
            }
        }
    }
}
