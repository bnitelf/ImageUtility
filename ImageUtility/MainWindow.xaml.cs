using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace ImageUtility
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isWindowLoaded = false;
        private string OutputFolderPath_LastProcess { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _isWindowLoaded = true;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load settings (isResize) and set to MainWindow.xaml
            // Set CheckBoxes
            chkResizeByPercent.IsChecked = Properties.Settings.Default.IsResizeByPercent;
            chkResizeByShorterSide.IsChecked = Properties.Settings.Default.IsResizeByShortestDimen;

            // Set TextBoxes
            txtResizePercentList.Text = Properties.Settings.Default.PercentList;
            txtResizeShorterSide.Text = Properties.Settings.Default.ResizeShorterSideToPx.ToString();

            chkJpgCompressionPercent.IsChecked = Properties.Settings.Default.IsEnableJpgCompression;
            txtJpgCompressionPercent.Text = Properties.Settings.Default.JpgCompressionQualityPercent.ToString();
        }

        private void BorderDropArea_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy; // Show copy cursor
            else
                e.Effects = DragDropEffects.None;
        }

        private void BorderDropArea_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                string dtNowStr = DateTime.Now.ToString("yyyy-MM-dd");
                string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"log_{dtNowStr}.txt");
                StringBuilder sbLog = new StringBuilder();
                bool hasError = false;
                int numFileProcessed = 0;
                string outputFolderPath = "";
                string filepathToProcess = "";

                bool isResizeByPercent = Properties.Settings.Default.IsResizeByPercent;
                bool isResizeByShortestDimen = Properties.Settings.Default.IsResizeByShortestDimen;

                bool isHeic = false;

                // Example: show file paths in MessageBox
                foreach (string file in files)
                {
                    filepathToProcess = file;

                    if (outputFolderPath == "")
                    {
                        outputFolderPath = Path.Combine(Path.GetDirectoryName(file), "Output");
                        OutputFolderPath_LastProcess = outputFolderPath;
                    }

                    if (file.EndsWith(".heic", StringComparison.InvariantCultureIgnoreCase)
                        || file.EndsWith(".heif", StringComparison.InvariantCultureIgnoreCase))
                    {
                        isHeic = true;
                        // Convert to jpg first
                        try
                        {
                            outputFolderPath = GetOutputFolderPath();
                            string tmpJpgFilepath = ConvertHeicToDestExtension(file, ".jpg", outputFolderPath);

                            filepathToProcess = tmpJpgFilepath;
                        }
                        catch (Exception ex)
                        {
                            hasError = true;
                            sbLog.AppendLine(ex.Message);
                            continue; // Skip further processing for this file
                        }
                    }

                    if (filepathToProcess.EndsWith(".png",StringComparison.InvariantCultureIgnoreCase) 
                        || filepathToProcess.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) 
                        || filepathToProcess.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase) 
                        || filepathToProcess.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            // Process image file
                            outputFolderPath = GetOutputFolderPath();
                            numFileProcessed++;
                            if (isResizeByPercent)
                            {
                                ResizeImageByPercent(filepathToProcess, outputFolderPath);
                            }
                            else
                            {
                                ResizeImageByShortestDimension(filepathToProcess, outputFolderPath);
                            }

                            if (isHeic)
                            {
                                // Delete temporary jpg file
                                File.Delete(filepathToProcess);
                            }
                        }
                        catch (Exception ex)
                        {
                            hasError = true;
                            sbLog.AppendLine(ex.Message);
                        }
                    }
                }

                if (hasError)
                {
                    File.AppendAllText(logFilePath, sbLog.ToString());
                    MessageBox.Show("Errors occurred. Please check the log file:\n" + logFilePath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (numFileProcessed > 0)
                {
                    MessageBox.Show("Image resizing completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Please drop image files", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void ResizeImageByPercent(string imageFilePath, string outputFolderPath)
        {
            string magickExecuteName = GetMagickCommand();

            Directory.CreateDirectory(outputFolderPath);

            // Execute ImageMagick command to resize image in 30%, 50%, 70%, 80%
            string resizePercentListStr = Properties.Settings.Default.PercentList;
            string[] resizePercents = resizePercentListStr.Split(',', StringSplitOptions.RemoveEmptyEntries);

            // Get JPG compression setting (if file is JPG)
            string commandJpgCompression = "";
            string filenameSuffix_JpgCompress = "";
            bool enableJpgCompression = Properties.Settings.Default.IsEnableJpgCompression;
            int jpgCompressionPercent = Properties.Settings.Default.JpgCompressionQualityPercent;
            if (enableJpgCompression && Regex.IsMatch(Path.GetExtension(imageFilePath), @"\.jpe?g"))
            {
                commandJpgCompression = $"-quality {jpgCompressionPercent} ";
                filenameSuffix_JpgCompress = $"_q{jpgCompressionPercent}";
            }
            
            StringBuilder sbErrorMessages = new StringBuilder();
            StringBuilder sbErrorMessagesFinal = new StringBuilder();

            foreach (string resizePercent in resizePercents)
            {
                string outputFilePath = Path.Combine(outputFolderPath, Path.GetFileNameWithoutExtension(imageFilePath) + $"_{resizePercent}p" + filenameSuffix_JpgCompress + Path.GetExtension(imageFilePath));
                
                string command = $"{magickExecuteName} \"{imageFilePath}\" -resize {resizePercent}% {commandJpgCompression} \"{outputFilePath}\"";

                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        //sbErrorMessages.AppendLine($"Error resizing to {resizePercent}%: {error}");
                    }
                }
            }

            if (sbErrorMessages.Length > 0)
            {
                sbErrorMessagesFinal.AppendLine("Error resize image: " + imageFilePath);
                sbErrorMessagesFinal.AppendLine(sbErrorMessages.ToString());

                throw new Exception(sbErrorMessagesFinal.ToString());
            }
        }

        private void ResizeImageByShortestDimension(string imageFilePath, string outputFolderPath)
        {
            string magickExecuteName = GetMagickCommand();

            Directory.CreateDirectory(outputFolderPath);

            // Execute ImageMagick command to resize shorter side to targetPx
            int targetPx = Properties.Settings.Default.ResizeShorterSideToPx;

            // Get JPG compression setting (if file is JPG)
            string commandJpgCompression = "";
            string filenameSuffix_JpgCompress = "";
            bool enableJpgCompression = Properties.Settings.Default.IsEnableJpgCompression;
            int jpgCompressionPercent = Properties.Settings.Default.JpgCompressionQualityPercent;
            if (enableJpgCompression && Regex.IsMatch(Path.GetExtension(imageFilePath), @"\.jpe?g"))
            {
                commandJpgCompression = $"-quality {jpgCompressionPercent} ";
                filenameSuffix_JpgCompress = $"_q{jpgCompressionPercent}";
            }

            StringBuilder sbErrorMessages = new StringBuilder();
            StringBuilder sbErrorMessagesFinal = new StringBuilder();


            string outputFilePath = Path.Combine(outputFolderPath, Path.GetFileNameWithoutExtension(imageFilePath) + $"_{targetPx}px" + filenameSuffix_JpgCompress + Path.GetExtension(imageFilePath));
            string command = $"{magickExecuteName} \"{imageFilePath}\" -resize \"{targetPx}x{targetPx}^>\" {commandJpgCompression} \"{outputFilePath}\"";

            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    //sbErrorMessages.AppendLine($"Error resizing to {resizePercent}%: {error}");
                }
            }
            

            if (sbErrorMessages.Length > 0)
            {
                sbErrorMessagesFinal.AppendLine("Error resize image: " + imageFilePath);
                sbErrorMessagesFinal.AppendLine(sbErrorMessages.ToString());

                throw new Exception(sbErrorMessagesFinal.ToString());
            }
        }

        private string GetMagickCommand()
        {
            string imageMagicFilepath = Properties.Settings.Default.ImageMagicFilepath;
            if (!string.IsNullOrEmpty(imageMagicFilepath) && File.Exists(imageMagicFilepath))
            {
                Console.WriteLine($"Using ImageMagick from settings: {imageMagicFilepath}");
                return $"\"{imageMagicFilepath}\"";
            }
            else if (IsMagickCommandAvailable())
            {
                Console.WriteLine("Using ImageMagick from system PATH.");
                return "magick";
            }
            else
            {
                throw new Exception("ImageMagick not found. Please set the correct path in settings.");
            }
        }

        private bool IsMagickSettingInSystemEnvironmentVariable()
        {
            string pathEnv = Environment.GetEnvironmentVariable("PATH")!;
            string[] paths = pathEnv.Split(';');
            foreach (string path in paths)
            {
                if (path.Trim().Contains("ImageMagick"))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsMagickCommandAvailable()
        {
            try
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "magick",
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo))
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private string GetOutputFolderPath()
        {
            string windowPictureFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string outputFolderPath = Path.Combine(windowPictureFolderPath, "ImageUtility");

            string settingOutputFolderPath = Properties.Settings.Default.OutputFolderPath;
            if (!string.IsNullOrEmpty(settingOutputFolderPath))
            {
                outputFolderPath = settingOutputFolderPath;
            }
            return outputFolderPath;
        }

        private string ConvertHeicToDestExtension(string heicFilePath, string destExtension, string outputFolderPath)
        {
            string magickExecuteName = GetMagickCommand();
            string filename = Path.GetFileName(heicFilePath);
            string outputFilename = Path.ChangeExtension(filename, destExtension);
            string outputFilePath = Path.Combine(outputFolderPath, outputFilename);
            string command = $"{magickExecuteName} \"{heicFilePath}\" \"{outputFilePath}\"";

            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Error converting HEIC to {destExtension}: {error}");
                }
            }

            return outputFilePath;
        }

        private void HyperlinkOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string outputFolderPath = GetOutputFolderPath();

            if (Directory.Exists(outputFolderPath))
            {
                Process.Start("explorer.exe", outputFolderPath);
            }
            else
            {
                MessageBox.Show("Folder not found: " + outputFolderPath);
            }
        }

        private void chkResizeByPercent_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!_isWindowLoaded) return;

            Properties.Settings.Default.IsResizeByPercent = true;
            Properties.Settings.Default.IsResizeByShortestDimen = false;
            Properties.Settings.Default.Save();

            chkResizeByShorterSide.IsChecked = false;
        }

        private void chkResizeByShorterSide_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!_isWindowLoaded) return;

            Properties.Settings.Default.IsResizeByShortestDimen = true;
            Properties.Settings.Default.IsResizeByPercent = false;
            Properties.Settings.Default.Save();

            chkResizeByPercent.IsChecked = false;
        }

        private void txtResizeShorterSide_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isWindowLoaded) return;

            string previousValue = Properties.Settings.Default.ResizeShorterSideToPx.ToString();

            // Validate if input is not an integer
            if (!int.TryParse(txtResizeShorterSide.Text, out int px))
            {
                txtResizeShorterSide.Background = Brushes.LightPink; // Indicate invalid input
                return;
            }

            txtResizeShorterSide.Background = Brushes.White; // Reset background if valid

            Properties.Settings.Default.ResizeShorterSideToPx = px;
            Properties.Settings.Default.Save();
        }

        private void txtResizePercentList_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isWindowLoaded) return;

            string text = txtResizePercentList.Text;
            string[] parts = text.Split(',');
            bool valid = true;
            foreach (var part in parts)
            {
                if (!int.TryParse(part.Trim(), out _))
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                Properties.Settings.Default.PercentList = text;
                Properties.Settings.Default.Save();
                txtResizePercentList.Background = Brushes.White;
            }
            else
            {
                txtResizePercentList.Background = Brushes.LightPink; // Indicate invalid input
            }
        }

        private void txtJpgCompressionPercent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isWindowLoaded) return;

            string previousValue = Properties.Settings.Default.JpgCompressionQualityPercent.ToString();

            // Validate if input is not an integer
            if (!int.TryParse(txtJpgCompressionPercent.Text, out int percent))
            {
                txtJpgCompressionPercent.Background = Brushes.LightPink; // Indicate invalid input
                return;
            }

            if (percent > 100)
            {
                percent = 100;
                txtJpgCompressionPercent.Text = "100";
            }

            txtJpgCompressionPercent.Background = Brushes.White; // Reset background if valid

            Properties.Settings.Default.JpgCompressionQualityPercent = percent;
            Properties.Settings.Default.Save();
        }

        private void chkJpgCompressionPercent_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!_isWindowLoaded) return;

            bool isChecked = ((CheckBox)sender).IsChecked == true;
            Properties.Settings.Default.IsEnableJpgCompression = isChecked;
            Properties.Settings.Default.Save();
        }

        private void chkJpgCompressionPercent_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isWindowLoaded) return;

            bool isChecked = ((CheckBox)sender).IsChecked == true;
            Properties.Settings.Default.IsEnableJpgCompression = isChecked;
            Properties.Settings.Default.Save();
        }
    }
}