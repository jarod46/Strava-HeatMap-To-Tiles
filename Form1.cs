using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.DevToolsProtocolExtension;
using System.Globalization;
using System.IO.Compression;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using WebView2.DevTools.Dom;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using HtmlElement = WebView2.DevTools.Dom.HtmlElement;

namespace StravaHeatMapToKMZ
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            webView21.NavigationCompleted += async (sender, args) =>
            {
                await webView21.ExecuteScriptAsync("let wintBut = document.querySelector('input[name=map_style][value=\"winter\"]');\r\n\twintBut.click();");
                await webView21.ExecuteScriptAsync("map.setZoom(12);");
                await Task.Delay(500);
                SetButtonsState(true);
            };

        }

        void SetButtonsState(bool enabled)
        {
            createKMZ.Enabled = enabled;
            updateKMZ.Enabled = enabled;
            webView21.Enabled = enabled;
            toggleElements.Enabled = enabled;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        NumberFormatInfo NFI
        {
            get
            {
                NumberFormatInfo nfi = new();
                nfi.NumberDecimalSeparator = ".";
                return nfi;
            }
        }

        Uri StravaHeatMapUri
        {
            get
            {
                return new Uri("https://www.strava.com/heatmap");
            }
        }

        bool CheckUri()
        {
            if (!webView21.Source.ToString().StartsWith(StravaHeatMapUri.ToString()))
            {
                MessageBox.Show("You must be on strava heatmap page !");
                webView21.Source = StravaHeatMapUri;
                return false;
            }
            return true;
        }

        async Task WaitForMapIdle(WebView2DevToolsContext devToolsContext)
        {
            string waitFunc = @"

                              function waitForMapIdle(timeout) {
                              map.on('idle', onMapIdle);

                              let fulfill;
                              let promise = new Promise(x => fulfill = x);
                              let timeoutId = setTimeout(onTimeoutDone, timeout);
                                
                              return promise;

                              function onTimeoutDone() {
                                map.off('idle', onMapIdle);     
                                fulfill();
                              }

                              function onMapIdle() {
                                map.off('idle', onMapIdle);
                                fulfill();
                                
                              }
                            }
                            waitForMapIdle(5000);
                            
                            ";
            await devToolsContext.EvaluateExpressionAsync(waitFunc);

        }

        enum UICommand
        {
            Show,
            Hide,
            Toggle,
        }

        async Task PrepareMap(WebView2DevToolsContext devToolsContext, UICommand command)
        {
            string[] helements = new string[] { "#sidebar", "#controls", "#global-header", ".mapboxgl-control-container", "#learn-more-modal", ".modal-backdrop", ".nav-bar" };
            foreach (var helement in helements)
            {
                var element = await devToolsContext.QuerySelectorAsync(helement);
                if (element != null)
                {
                    bool hide = false;
                    switch (command)
                    {
                        case UICommand.Show:
                            hide = false;
                            break;
                        case UICommand.Hide:
                            hide = true;
                            break;
                        case UICommand.Toggle:
                            hide = await element.GetIsHiddenAsync();
                            hide = !hide;
                            break;
                    }
                    await element.SetHiddenAsync(hide);
                    if (hide)
                    {
                        var style = await element.GetStyleAsync();
                        await style.SetPropertyAsync("height", "0");
                        await style.SetPropertyAsync("border-bottom", "0");
                    }
                }
            }

            string resizeScript = "window.dispatchEvent(new Event('resize'));\r\n\tmap.resize();";
            await webView21.ExecuteScriptAsync(resizeScript);
            await webView21.ExecuteScriptAsync("map.setZoom(15);");
        }

        async Task FitBounds(WebView2DevToolsContext devToolsContext, double west, double south, double east, double north)
        {
            var nfi = NFI;
            var fitScript = "map.fitBounds([[" + west.ToString(nfi) + "," + south.ToString(nfi) + "],[" + east.ToString(nfi) + "," + north.ToString(nfi) + "]], {animate : false});";
            await webView21.ExecuteScriptAsync(fitScript);
            await WaitForMapIdle(devToolsContext);
            await Task.Delay(100);
        }

        private async void createKMZ_Click(object sender, EventArgs e)
        {
            if (!CheckUri())
                return;

            webView21.Enabled = false;
            SetButtonsState(false);
            await using var devToolsContext = await webView21.CoreWebView2.CreateDevToolsContextAsync();



            var bScript = "(value) => ({east: map.getBounds().getEast(), west: map.getBounds().getWest(), north: map.getBounds().getNorth(), south: map.getBounds().getSouth()}) ";
            var bounds = await devToolsContext.EvaluateFunctionAsync<dynamic>(bScript);

            await PrepareMap(devToolsContext, UICommand.Hide);

            var cScript = "(value) => ({lng: map.getCenter().lng, lat: map.getCenter().lat}) ";
            var center = await devToolsContext.EvaluateFunctionAsync<dynamic>(cScript);
            var bounds15 = await devToolsContext.EvaluateFunctionAsync<dynamic>(bScript);
            var latSize = bounds15.north - bounds15.south;
            var lonSize = bounds15.east - bounds15.west;

            MemoryStream mZipStream = new();
            ZipArchive archive = new(mZipStream, ZipArchiveMode.Update, true);
            ZipArchiveEntry kmlEntry = archive.CreateEntry("doc.kml");

            XmlWriterSettings xmlWriterSettings = new();
            xmlWriterSettings.NewLineOnAttributes = true;
            xmlWriterSettings.Indent = true;
            XmlWriter kmlWriter = XmlWriter.Create(kmlEntry.Open(), xmlWriterSettings);
            kmlWriter.WriteStartElement("kml");
            kmlWriter.WriteStartElement("Document");
            kmlWriter.WriteElementString("name", "strava heatmap x");
            kmlWriter.WriteStartElement("LookAt");
            var nfi = NFI;
            kmlWriter.WriteElementString("longitude", center.lng.ToString(nfi));
            kmlWriter.WriteElementString("latitude", center.lat.ToString(nfi));
            kmlWriter.WriteElementString("range", "1000");
            kmlWriter.WriteEndElement();
            int id = 0;
            int count = 0;
            progressBar1.Value = 0;
            for (double x = bounds.west; x <= bounds.east; x += lonSize)
            {
                for (double y = bounds.north; y >= bounds.south; y -= latSize)
                {
                    count++;
                }
            }
            progressBar1.Maximum = count;
            for (double x = bounds.west; x <= bounds.east; x += lonSize)
            {
                for (double y = bounds.north; y >= bounds.south; y -= latSize)
                {
                    progressBar1.Value++;
                    id++;
                    var west = x;
                    var east = x + lonSize;
                    var north = y;
                    var south = y - latSize;
                    await FitBounds(devToolsContext, west, south, east, north);
                    var screenPath = "files/tile" + id + ".jpg";
                    var screenEntry = archive.CreateEntry(screenPath);
                    await webView21.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Jpeg, screenEntry.Open());

                    kmlWriter.WriteStartElement("GroundOverlay");
                    kmlWriter.WriteStartElement("Icon");
                    kmlWriter.WriteElementString("href", screenPath);
                    kmlWriter.WriteEndElement();
                    kmlWriter.WriteStartElement("LatLonBox");
                    kmlWriter.WriteElementString("north", north.ToString(nfi));
                    kmlWriter.WriteElementString("south", south.ToString(nfi));
                    kmlWriter.WriteElementString("east", east.ToString(nfi));
                    kmlWriter.WriteElementString("west", west.ToString(nfi));
                    kmlWriter.WriteEndElement();
                    kmlWriter.WriteEndElement();

                }
            }
            kmlWriter.WriteEndElement();
            kmlWriter.WriteEndElement();
            kmlWriter.Flush();
            kmlWriter.Dispose();

            archive.Dispose();
            SaveFileDialog saveFileDialog = new();
            saveFileDialog.DefaultExt = "kmz";
            saveFileDialog.Filter = "kmz files (*.kmz)|*.kmz";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var kmzPath = saveFileDialog.FileName;
                FileStream fileStream = new(kmzPath, FileMode.Create);
                mZipStream.Seek(0, SeekOrigin.Begin);
                mZipStream.CopyTo(fileStream);
                mZipStream.Close();
                fileStream.Close();
            }
            await PrepareMap(devToolsContext, UICommand.Show);
            await devToolsContext.DisposeAsync();
            webView21.Enabled = true;
            SetButtonsState(true);
        }

        class KMLTile
        {
            public string screenPath = string.Empty;
            public double north = 0;
            public double south = 0;
            public double west = 0;
            public double east = 0;
        }

        private async void updateKMZ_ClickAsync(object sender, EventArgs e)
        {
            if (!CheckUri())
                return;


            OpenFileDialog openFileDialog = new();
            openFileDialog.DefaultExt = "kmz";
            openFileDialog.Filter = "kmz files (*.kmz)|*.kmz";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                webView21.Enabled = false;
                SetButtonsState(false);
                FileStream kmzFileStream = new(openFileDialog.FileName, FileMode.Open);
                ZipArchive kmzArchive = new(kmzFileStream, ZipArchiveMode.Update);
                var kmlEntry = kmzArchive.GetEntry("doc.kml");
                if (kmlEntry != null)
                {
                    XmlReader kmlReader = XmlReader.Create(kmlEntry.Open());
                    List<KMLTile> tiles = new();
                    while (kmlReader.Read())
                    {
                        if (kmlReader.NodeType == XmlNodeType.Element && kmlReader.Name == "GroundOverlay")
                        {
                            string screenPath = string.Empty;
                            double north = 0, south = 0, east = 0, west = 0;
                            while (kmlReader.NodeType != XmlNodeType.EndElement || kmlReader.Name != "GroundOverlay")
                            {
                                kmlReader.Read();
                                //MessageBox.Show(kmlReader.Name);
                                if (kmlReader.NodeType == XmlNodeType.Element)
                                {
                                    switch (kmlReader.Name)
                                    {
                                        case "href":
                                            kmlReader.Read();
                                            screenPath = kmlReader.Value;
                                            break;
                                        case "north":
                                            kmlReader.Read();
                                            double.TryParse(kmlReader.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out north);
                                            break;
                                        case "south":
                                            kmlReader.Read();
                                            double.TryParse(kmlReader.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out south);
                                            break;
                                        case "east":
                                            kmlReader.Read();
                                            double.TryParse(kmlReader.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out east);
                                            break;
                                        case "west":
                                            kmlReader.Read();
                                            double.TryParse(kmlReader.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out west);
                                            break;
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(screenPath) && north != 0 && south != 0 && west != 0 && east != 0)
                            {
                                var tile = new KMLTile();
                                tile.screenPath = screenPath;
                                tile.north = north;
                                tile.south = south;
                                tile.east = east;
                                tile.west = west;
                                tiles.Add(tile);
                            }
                        }

                    }
                    await using var devToolsContext = await webView21.CoreWebView2.CreateDevToolsContextAsync();
                    await PrepareMap(devToolsContext, UICommand.Hide);
                    progressBar1.Value = 0;
                    progressBar1.Maximum = tiles.Count;
                    foreach (var tile in tiles)
                    {
                        progressBar1.Value++;
                        await FitBounds(devToolsContext, tile.west, tile.south, tile.east, tile.north);
                        var screenEntry = kmzArchive.GetEntry(tile.screenPath);
                        if (screenEntry != null)
                        {
                            var screenStream = screenEntry.Open();
                            screenStream.SetLength(0);
                            await webView21.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Jpeg, screenStream);
                        }
                    }
                    await PrepareMap(devToolsContext, UICommand.Show);
                    await devToolsContext.DisposeAsync();
                }
                kmzArchive.Dispose();
                SetButtonsState(true);
                webView21.Enabled = true;
            }
        }

        async private void toggleElements_Click(object sender, EventArgs e)
        {
            await using var devToolsContext = await webView21.CoreWebView2.CreateDevToolsContextAsync();

            await PrepareMap(devToolsContext, UICommand.Toggle);

            await devToolsContext.DisposeAsync();
        }
    }


}