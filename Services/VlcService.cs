using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace VideoControlAPI.Services
{
    /// <summary>
    /// Controla VLC usando su interfaz HTTP (puerto 8080).
    /// VLC debe arrancarse con: vlc.exe --intf http --http-port 8080 --http-password 1234
    /// </summary>
    public class VlcService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<VlcService> _logger;
        private readonly HttpClient _http;
        private const string VLC_URL = "http://localhost:8080";
        private const string VLC_PASSWORD = "1234"; // coincide con --http-password

        public VlcService(IConfiguration config, ILogger<VlcService> logger)
        {
            _config = config;
            _logger = logger;
            _http = new HttpClient();
            var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{VLC_PASSWORD}"));
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", creds);
            _http.Timeout = TimeSpan.FromSeconds(3);
        }

        // ── Lanza VLC con interfaz HTTP ────────────────────────────────────
        public void EnsureVlcRunning()
        {
            var running = Process.GetProcessesByName("vlc").Length > 0;
            if (running) return;

            var vlcPath = _config["VideoSettings:VlcPath"]
                ?? @"C:\Program Files\VideoLAN\VLC\vlc.exe";

            if (!File.Exists(vlcPath))
            {
                _logger.LogWarning("VLC no encontrado en {Path}", vlcPath);
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = vlcPath,
                Arguments = "--intf http --http-port 8080 --http-password 1234 --no-video-title-show",
                UseShellExecute = false
            };
            Process.Start(psi);
            Thread.Sleep(1500); // esperar que VLC arranque
            _logger.LogInformation("VLC iniciado");
        }

        // ── Reproduce un archivo ───────────────────────────────────────────
        public async Task<bool> PlayFile(string filePath)
        {
            try
            {
                EnsureVlcRunning();
                var encoded = Uri.EscapeDataString(filePath);
                var url = $"{VLC_URL}/requests/status.xml?command=in_play&input={encoded}";
                var resp = await _http.GetAsync(url);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reproducir {File}", filePath);
                return false;
            }
        }

        // ── Pausa / reanuda ────────────────────────────────────────────────
        public async Task<bool> Pause()
        {
            return await SendCommand("pl_pause");
        }

        // ── Detiene reproducción ───────────────────────────────────────────
        public async Task<bool> Stop()
        {
            return await SendCommand("pl_stop");
        }

        // ── Siguiente en la lista interna de VLC ──────────────────────────
        public async Task<bool> Next()
        {
            return await SendCommand("pl_next");
        }

        // ── Obtiene el estado actual de VLC ───────────────────────────────
        public async Task<string> GetState()
        {
            try
            {
                var resp = await _http.GetAsync($"{VLC_URL}/requests/status.json");
                if (!resp.IsSuccessStatusCode) return "stopped";
                var json = await resp.Content.ReadAsStringAsync();
                if (json.Contains("\"state\":\"playing\"")) return "playing";
                if (json.Contains("\"state\":\"paused\""))  return "paused";
                return "stopped";
            }
            catch { return "stopped"; }
        }

        // ── Ajusta el volumen (0-200) ─────────────────────────────────────
        public async Task<bool> SetVolume(int volume)
        {
            volume = Math.Clamp(volume, 0, 200);
            return await SendCommand($"volume&val={volume}");
        }

        // ── Fullscreen toggle ─────────────────────────────────────────────
        public async Task<bool> ToggleFullscreen()
        {
            return await SendCommand("fullscreen");
        }

        // ── Comando genérico ──────────────────────────────────────────────
        private async Task<bool> SendCommand(string command)
        {
            try
            {
                var resp = await _http.GetAsync($"{VLC_URL}/requests/status.xml?command={command}");
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error VLC command {Cmd}", command);
                return false;
            }
        }
    }
}
