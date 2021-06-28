﻿using Microsoft.Win32;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Hashificator
{
    public partial class MainWindow : Window
    {
        [DllImport("user32")] public static extern int FlashWindow(IntPtr hwnd, bool bInvert);

        private Dictionary<string, HashCollection> _outputDict;

        public MainWindow()
        {
            InitializeComponent();
        }

        private string CalculateHash<T>(string path) where T : IDigest, new()
        {
            IDigest hash = new T();

            byte[] result = new byte[hash.GetDigestSize()];

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
            {
                byte[] buffer = new byte[4092];
                int bytesRead;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    hash.BlockUpdate(buffer, 0, bytesRead);
                }

                hash.DoFinal(result, 0);
            }
            return BitConverter.ToString(result).Replace("-", "");
        }

        private string CalculateSha3Hash(string path, int bitLength)
        {
            var hashAlgorithm = new Sha3Digest(bitLength);

            byte[] result = new byte[bitLength / 8];

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
            {
                byte[] buffer = new byte[4092];
                int bytesRead;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    hashAlgorithm.BlockUpdate(buffer, 0, bytesRead);
                }

                hashAlgorithm.DoFinal(result, 0);
            }
            return BitConverter.ToString(result).Replace("-", "");
        }

        private void CalculateTab_AddButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Select file(s)",
                Filter = "All files (*.*) | *.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    CalculateTab_InputFileListBox.Items.Add(fileName);
                }
            }
        }

        private void CalculateTab_RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in CalculateTab_InputFileListBox.SelectedItems.Cast<object>().ToArray())
            {
                CalculateTab_InputFileListBox.Items.Remove(item);
            }
        }

        private void CalculateTab_CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            CalculateTab_ScannedFileListBox.Items.Clear();

            _outputDict = new Dictionary<string, HashCollection>();

            if (CalculateTab_InputFileListBox.Items.Count == 0)
            {
                MessageBox.Show("Please add at least one file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var inputList = new string[CalculateTab_InputFileListBox.Items.Count];
            CalculateTab_InputFileListBox.Items.CopyTo(inputList, 0);

            var checkBoxes = new HashSelection();

            checkBoxes.MD2 = CalculateTab_ScanMD2CheckBox.IsChecked ?? false;
            checkBoxes.MD4 = CalculateTab_ScanMD4CheckBox.IsChecked ?? false;
            checkBoxes.MD5 = CalculateTab_ScanMD5CheckBox.IsChecked ?? false;

            checkBoxes.Sha1 = CalculateTab_ScanSha1CheckBox.IsChecked ?? false;
            checkBoxes.Sha224 = CalculateTab_ScanSha224CheckBox.IsChecked ?? false;
            checkBoxes.Sha256 = CalculateTab_ScanSha256CheckBox.IsChecked ?? false;
            checkBoxes.Sha384 = CalculateTab_ScanSha384CheckBox.IsChecked ?? false;
            checkBoxes.Sha512 = CalculateTab_ScanSha512CheckBox.IsChecked ?? false;

            checkBoxes.Sha3_224 = CalculateTab_ScanSha3_224CheckBox.IsChecked ?? false;
            checkBoxes.Sha3_256 = CalculateTab_ScanSha3_256CheckBox.IsChecked ?? false;
            checkBoxes.Sha3_384 = CalculateTab_ScanSha3_384CheckBox.IsChecked ?? false;
            checkBoxes.Sha3_512 = CalculateTab_ScanSha3_512CheckBox.IsChecked ?? false;

            var worker = new BackgroundWorker();

            worker.DoWork += workerWork;

            void workerWork(object obj, DoWorkEventArgs e)
            {
                Dispatcher.BeginInvoke((Action)(() => EnableAllCalculateTabButtons(false)));

                foreach (var item in inputList)
                {
                    var hashCollection = new HashCollection();

                    if (checkBoxes.MD2) hashCollection.MD2 = CalculateHash<MD2Digest>(item);
                    if (checkBoxes.MD4) hashCollection.MD4 = CalculateHash<MD4Digest>(item);
                    if (checkBoxes.MD5) hashCollection.MD5 = CalculateHash<MD5Digest>(item);

                    if (checkBoxes.Sha1) hashCollection.Sha1 = CalculateHash<Sha1Digest>(item);
                    if (checkBoxes.Sha224) hashCollection.Sha224 = CalculateHash<Sha224Digest>(item);
                    if (checkBoxes.Sha256) hashCollection.Sha256 = CalculateHash<Sha256Digest>(item);
                    if (checkBoxes.Sha384) hashCollection.Sha384 = CalculateHash<Sha384Digest>(item);
                    if (checkBoxes.Sha512) hashCollection.Sha512 = CalculateHash<Sha512Digest>(item);

                    if (checkBoxes.Sha3_224) hashCollection.Sha3_224 = CalculateSha3Hash(item, 224);
                    if (checkBoxes.Sha3_256) hashCollection.Sha3_256 = CalculateSha3Hash(item, 256);
                    if (checkBoxes.Sha3_384) hashCollection.Sha3_384 = CalculateSha3Hash(item, 384);
                    if (checkBoxes.Sha3_512) hashCollection.Sha3_512 = CalculateSha3Hash(item, 512);

                    _outputDict[item] = hashCollection;

                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        CalculateTab_ScannedFileListBox.Items.Add(item);
                        CalculateTab_InputFileListBox.Items.Remove(item);
                        CalculateTab_ProgressBar.Value += 100 / inputList.Count();
                    }));
                }

                Dispatcher.BeginInvoke((Action)(() => EnableAllCalculateTabButtons(true)));
            }

            worker.RunWorkerAsync();
        }

        private void EnableAllCalculateTabButtons(bool yes)
        {
            CalculateTab_ProgressBar.Value = 0;
            CalculateTab_ProgressBar.IsIndeterminate = yes ? false : true;
            TaskbarItemInfo.ProgressState = yes ? System.Windows.Shell.TaskbarItemProgressState.None : System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
            CalculateTab_AddButton.IsEnabled = yes;
            CalculateTab_RemoveButton.IsEnabled = yes;
            CalculateTab_CalculateButton.IsEnabled = yes;

            CalculateTab_ScanMD2CheckBox.IsEnabled = yes;
            CalculateTab_ScanMD4CheckBox.IsEnabled = yes;
            CalculateTab_ScanMD5CheckBox.IsEnabled = yes;

            CalculateTab_ScanSha1CheckBox.IsEnabled = yes;
            CalculateTab_ScanSha224CheckBox.IsEnabled = yes;
            CalculateTab_ScanSha256CheckBox.IsEnabled = yes;
            CalculateTab_ScanSha384CheckBox.IsEnabled = yes;
            CalculateTab_ScanSha512CheckBox.IsEnabled = yes;

            CalculateTab_ScanSha3_224CheckBox.IsEnabled = yes;
            CalculateTab_ScanSha3_256CheckBox.IsEnabled = yes;
            CalculateTab_ScanSha3_384CheckBox.IsEnabled = yes;
            CalculateTab_ScanSha3_512CheckBox.IsEnabled = yes;

            if (yes)
            {
                FlashWindow(new WindowInteropHelper(this).Handle, true);
                MessageBox.Show("Job's done!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CalculateTab_ScannedFileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0)
            {
                var hashes = _outputDict[(string)e.AddedItems[0]];

                CalculateTab_MD2Result.Text = hashes.MD2;
                CalculateTab_MD4Result.Text = hashes.MD4;
                CalculateTab_MD5Result.Text = hashes.MD5;

                CalculateTab_Sha1Result.Text = hashes.Sha1;
                CalculateTab_Sha224Result.Text = hashes.Sha224;
                CalculateTab_Sha256Result.Text = hashes.Sha256;
                CalculateTab_Sha384Result.Text = hashes.Sha384;
                CalculateTab_Sha512Result.Text = hashes.Sha512;

                CalculateTab_Sha3_224Result.Text = hashes.Sha3_224;
                CalculateTab_Sha3_256Result.Text = hashes.Sha3_256;
                CalculateTab_Sha3_384Result.Text = hashes.Sha3_384;
                CalculateTab_Sha3_512Result.Text = hashes.Sha3_512;
            }
            else
            {
                CalculateTab_MD2Result.Text = string.Empty;
                CalculateTab_MD4Result.Text = string.Empty;
                CalculateTab_MD5Result.Text = string.Empty;

                CalculateTab_Sha1Result.Text = string.Empty;
                CalculateTab_Sha224Result.Text = string.Empty;
                CalculateTab_Sha256Result.Text = string.Empty;
                CalculateTab_Sha384Result.Text = string.Empty;
                CalculateTab_Sha512Result.Text = string.Empty;

                CalculateTab_Sha3_224Result.Text = string.Empty;
                CalculateTab_Sha3_256Result.Text = string.Empty;
                CalculateTab_Sha3_384Result.Text = string.Empty;
                CalculateTab_Sha3_512Result.Text = string.Empty;
            }
        }
    }
}