using System;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace OllamaChatUI
{
    public partial class Form1 : Form
    {
        private readonly HttpClient httpClient = new HttpClient();
        private PrivateFontCollection privateFonts = new PrivateFontCollection();

        // load font from memory
        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);

        public Form1()
        {
            InitializeComponent();
            LoadCustomFont();

            RoundControl(txtUserInput, 6);
            RoundControl(txtResponse, 6);

            this.AcceptButton = btnSend;  // Enter With KeyBoard
        }


        private void RoundControl(Control control, int radius)
        {
            Rectangle bounds = new Rectangle(0, 0, control.Width, control.Height);
            GraphicsPath path = new GraphicsPath();

            int d = radius * 2;
            path.StartFigure();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            control.Region = new Region(path);
        }

        private void LoadCustomFont()
        {
            try
            {
                string resourceName = "OllamaChatUI.Fonts.Vazirmatn-Medium.ttf"; // Namespace + Folder + FileName 

                using (Stream fontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (fontStream == null)
                    {
                        // debugging
                        string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                        string allResources = string.Join("\n", resources);
                        MessageBox.Show($"Font not found. Available resources:\n{allResources}");
                        return;
                    }

                    // read font byte
                    byte[] fontData = new byte[fontStream.Length];
                    fontStream.Read(fontData, 0, (int)fontStream.Length);

                    // add font temp & clear memory
                    uint dummy = 0;
                    IntPtr fontPtr = Marshal.AllocCoTaskMem(fontData.Length);
                    Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
                    AddFontMemResourceEx(fontPtr, (uint)fontData.Length, IntPtr.Zero, ref dummy);
                    privateFonts.AddMemoryFont(fontPtr, fontData.Length);
                    Marshal.FreeCoTaskMem(fontPtr);

                    // Apply
                    if (privateFonts.Families.Length > 0)
                    {
                        Font customFont = new Font(privateFonts.Families[0], 11f, FontStyle.Regular);

                        txtUserInput.Font = customFont;
                        txtResponse.Font = customFont;
                        btnSend.Font = customFont;
                    }
                    else
                    {
                        MessageBox.Show("The font was not loaded successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading font: " + ex.Message);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string userMessage = txtUserInput.Text.Trim();
            if (string.IsNullOrEmpty(userMessage)) return;

            ApplyDirectionality(txtUserInput, userMessage);
            ApplyDirectionality(txtResponse, userMessage);

            txtResponse.Text += $"👤 You: {userMessage}\n";

            if (privateFonts.Families.Length > 0)
                txtResponse.Font = new Font(privateFonts.Families[0], 11f, FontStyle.Regular);

            var requestBody = new
            {
                model = "gemma3:1b",
                stream = false,
                messages = new[]
                {
                    new { role = "user", content = userMessage }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("http://localhost:11434/api/chat", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                dynamic json = JsonConvert.DeserializeObject(responseBody);
                string responseText = json.message.content;
                ApplyDirectionality(txtResponse, responseText);

                txtResponse.Text += $"🤖 Ollama: {responseText}\n\n";

                if (privateFonts.Families.Length > 0)
                    txtResponse.Font = new Font(privateFonts.Families[0], 11f, FontStyle.Regular);

                txtUserInput.Clear();

            }
            catch (Exception ex)
            {
                txtResponse.Text += "Error Connection Ollama: " + ex.Message;
            }
        }

        private bool IsPersian(string text)
        {
            // check persian char
            foreach (char c in text)
            {
                if (c >= 0x0600 && c <= 0x06FF) return true;
            }
            return false;
        }

        private void ApplyDirectionality(Control control, string text)
        {
            if (IsPersian(text))
            {
                control.RightToLeft = RightToLeft.Yes;

                // MultiLine Fix
                if (control is TextBox tb && tb.Multiline)
                    tb.TextAlign = HorizontalAlignment.Right;
            }
            else
            {
                control.RightToLeft = RightToLeft.No;
                if (control is TextBox tb && tb.Multiline)
                    tb.TextAlign = HorizontalAlignment.Left;
            }
        }

    }
}