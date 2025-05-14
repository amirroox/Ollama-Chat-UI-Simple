using System;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace OllamaChatUI
{
    public partial class Form1: Form
    {
        private readonly HttpClient httpClient = new HttpClient();

        public Form1()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string userMessage = txtUserInput.Text.Trim();
            if (string.IsNullOrEmpty(userMessage)) return;

            txtResponse.Text += $"👤 شما: {userMessage}\n";

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

                txtResponse.Text += $"🤖 Ollama: {responseText}\n\n";
                txtUserInput.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در ارتباط با Ollama: " + ex.Message);
            }
        }
    }
}
