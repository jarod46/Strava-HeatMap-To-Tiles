using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.DevToolsProtocolExtension;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using WebView2.DevTools.Dom;
using static System.Windows.Forms.DataFormats;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using HtmlElement = WebView2.DevTools.Dom.HtmlElement;

namespace StravaHeatMapToKMZ
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            mapStyle.SelectedIndex = 0;
            activityType.SelectedIndex = 0;
            SetButtonsState(false);
            webView21.NavigationCompleted += async (sender, args) =>
            {
                await webView21.ExecuteScriptAsync("document.querySelector('input[name=map_style][value=\"winter\"]').click();");

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
            createTiles.Enabled = enabled;
            createKarooTiles.Enabled = enabled;
            updateTiles.Enabled = enabled;
            toggleElements.Enabled = enabled;
            mapStyle.Enabled = enabled;
            activityType.Enabled = enabled;
            tileSize.Enabled = enabled;
            tileZoom.Enabled = enabled;
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

        async Task PrepareMap(WebView2DevToolsContext devToolsContext, Microsoft.Web.WebView2.WinForms.WebView2 webView, UICommand command, int tileSize = 1000, int zoom = 15)
        {
            webView.Size = new Size(tileSize, tileSize);

            //string[] helements = new string[] { "#sidebar", "#controls", "#global-header", ".mapboxgl-control-container", "#learn-more-modal", ".modal-backdrop", ".nav-bar" };
            //foreach (var helement in helements)
            //{
            //    var element = await devToolsContext.QuerySelectorAsync(helement);
            //    if (element != null)
            //    {
            //        bool hide = false;
            //        switch (command)
            //        {
            //            case UICommand.Show:
            //                hide = false;
            //                break;
            //            case UICommand.Hide:
            //                hide = true;
            //                break;
            //            case UICommand.Toggle:
            //                hide = await element.GetIsHiddenAsync();
            //                hide = !hide;
            //                break;
            //        }
            //        await element.SetHiddenAsync(hide);
            //        if (hide)
            //        {
            //            var style = await element.GetStyleAsync();
            //            await style.SetPropertyAsync("height", "0");
            //            await style.SetPropertyAsync("border-bottom", "0");
            //        }
            //    }
            //}

            //string resizeScript = "window.dispatchEvent(new Event('resize'));\r\n\tmap.resize();";
            //await webView.ExecuteScriptAsync(resizeScript);
            //await webView.ExecuteScriptAsync("map.setZoom(" + zoom + ");");

            var prepareScript = @"let elements = [""#sidebar"", ""#controls"", ""#global-header"", "".mapboxgl-control-container"", ""#learn-more-modal"", "".modal-backdrop""];
	                                elements.forEach((elem) => {
		                                let dElem = document.querySelector(elem);
		                                if (dElem != null)
		                                dElem.parentNode.removeChild(dElem);
	                                });
	                                window.dispatchEvent(new Event('resize'));
	                                map.resize();";
            await webView.ExecuteScriptAsync(prepareScript);
        }

        async Task FitBounds(WebView2DevToolsContext devToolsContext, Microsoft.Web.WebView2.WinForms.WebView2 webview, double west, double south, double east, double north)
        {
            var nfi = NFI;
            var fitScript = "map.fitBounds([[" + west.ToString(nfi) + "," + south.ToString(nfi) + "],[" + east.ToString(nfi) + "," + north.ToString(nfi) + "]], {animate : false});";
            await webview.ExecuteScriptAsync(fitScript);
            await Task.Delay(10);
            await WaitForMapIdle(devToolsContext);
            //await Task.Delay(100);
        }

        private async void createKMZ_Click(object sender, EventArgs e)
        {
            if (!CheckUri())
                return;

            SetButtonsState(false);

            var viewInfos = await GetTilesInfosForCurrentView((int)tileZoom.Value);

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
            kmlWriter.WriteElementString("longitude", viewInfos.centerLng.ToString(nfi));
            kmlWriter.WriteElementString("latitude", viewInfos.centerLat.ToString(nfi));
            kmlWriter.WriteElementString("range", "1000");
            kmlWriter.WriteEndElement();

            var onTileCreate = delegate (MemoryStream stream, Tile tile)
            {
                var screenPath = "files/tile" + tile.x1 + "_" + tile.y1 + ".jpg";
                var screenEntry = archive.CreateEntry(screenPath);
                var streamEntry = screenEntry.Open();
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(streamEntry);
                streamEntry.Close();

                var worldCoord = Tile.TileToWorldPos(tile.x1, tile.y1, (int)tileZoom.Value);
                var worldCoordMax = Tile.TileToWorldPos(tile.x2, tile.y2, (int)tileZoom.Value);
                var west = worldCoord.X;
                var east = worldCoordMax.X;
                var north = worldCoord.Y;
                var south = worldCoordMax.Y;
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
            };

            var result = await CreateTiles(onTileCreate, TileFormat.jpg, (int)tileSize.Value, (int)tileZoom.Value);

            kmlWriter.WriteEndElement();
            kmlWriter.WriteEndElement();
            kmlWriter.Flush();
            kmlWriter.Dispose();

            archive.Dispose();
            if (!result)
            {
                mZipStream.Dispose();
                goto end;
            }
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

        end:
            SetButtonsState(true);
        }

        //class KMLTile
        //{
        //    public string screenPath = string.Empty;
        //    public double north = 0;
        //    public double south = 0;
        //    public double west = 0;
        //    public double east = 0;
        //}

        private async void updateKMZ_ClickAsync(object sender, EventArgs e)
        {
            if (!CheckUri())
                return;

            OpenFileDialog openFileDialog = new();
            openFileDialog.DefaultExt = "kmz";
            openFileDialog.Filter = "kmz files (*.kmz)|*.kmz";
            int updated = 0;
            int usedTileSize = -1;
            var usedFormat = TileFormat.none;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                FileStream kmzFileStream = new(openFileDialog.FileName, FileMode.Open);
                ZipArchive kmzArchive = new(kmzFileStream, ZipArchiveMode.Update);
                var kmlEntry = kmzArchive.GetEntry("doc.kml");
                if (kmlEntry != null)
                {

                    XmlReader kmlReader = XmlReader.Create(kmlEntry.Open());
                    List<Tile> tiles = new();
                    while (kmlReader.Read())
                    {
                        if (abordWork)
                        {
                            tiles.Clear();
                            break;
                        }
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
                                var tile = new Tile(north, south, east, west, 15);
                                tile.tag = screenPath;
                                tiles.Add(tile);

                                if (usedFormat == TileFormat.none)
                                {
                                    if (tile.tag.EndsWith("jpg"))
                                        usedFormat = TileFormat.jpg;
                                    if (tile.tag.EndsWith("png"))
                                        usedFormat = TileFormat.png;
                                }
                                if (usedTileSize < 0)
                                {
                                    var screenEntry = kmzArchive.GetEntry(tile.tag);
                                    if (screenEntry != null)
                                    {
                                        var screenStream = screenEntry.Open();
                                        var bitmap = new Bitmap(screenStream);
                                        usedTileSize = bitmap.Size.Height;
                                        screenStream.Close();
                                    }
                                }
                            }
                        }

                    }

                    if (usedTileSize < 0)
                    {
                        MessageBox.Show("Can't get tile size");
                        goto end;
                    }
                    if (usedFormat == TileFormat.none)
                    {
                        MessageBox.Show("Unable to get tile image format");
                        goto end;
                    }

                    var onTileCreate = delegate (MemoryStream stream, Tile tile)
                    {
                        var screenEntry = kmzArchive.GetEntry(tile.tag);
                        if (screenEntry != null)
                        {
                            var screenStream = screenEntry.Open();
                            screenStream.SetLength(0);
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.CopyTo(screenStream);
                            screenStream.Close();
                            updated++;
                        }
                    };

                    var result = await CreateTiles(onTileCreate, usedFormat, usedTileSize, 15, tiles);
                }
            end:
                kmzArchive.Dispose();
                SetButtonsState(true);
                MessageBox.Show(updated + " tiles udpated !");
            }
        }

        async private void toggleElements_Click(object sender, EventArgs e)
        {
            await using var devToolsContext = await webView21.CoreWebView2.CreateDevToolsContextAsync();

            await PrepareMap(devToolsContext, webView21, UICommand.Toggle);

            await devToolsContext.DisposeAsync();
        }

        string lastTempFolder = string.Empty;



        class ViewInfos
        {
            public List<Tile> tiles = new List<Tile>();
            public double centerLat = 0;
            public double centerLng = 0;
        }

        async Task<ViewInfos> GetTilesInfosForCurrentView(int zoom)
        {
            var viewInfos = new ViewInfos();

            await using var devToolsContext = await webView21.CoreWebView2.CreateDevToolsContextAsync();

            var bScript = "(value) => ({east: map.getBounds().getEast(), west: map.getBounds().getWest(), north: map.getBounds().getNorth(), south: map.getBounds().getSouth()}) ";
            var bounds = await devToolsContext.EvaluateFunctionAsync<dynamic>(bScript);

            var minPoint = Tile.WorldToTilePos(bounds.west, bounds.north, zoom);
            var maxPoint = Tile.WorldToTilePos(bounds.east, bounds.south, zoom);

            for (double x = Math.Floor(minPoint.X); x <= Math.Floor(maxPoint.X); x++)
            {
                for (double y = Math.Floor(minPoint.Y); y <= Math.Floor(maxPoint.Y); y++)
                {
                    viewInfos.tiles.Add(new Tile(new Point((int)x, (int)y), new Point((int)x + 1, (int)y + 1), zoom));
                }
            }

            var cScript = "(value) => ({lng: map.getCenter().lng, lat: map.getCenter().lat}) ";
            var center = await devToolsContext.EvaluateFunctionAsync<dynamic>(cScript);
            viewInfos.centerLat = center.lat;
            viewInfos.centerLng = center.lng;

            await devToolsContext.DisposeAsync();

            return viewInfos;
        }

        enum TileFormat
        {
            png,
            jpg,
            none,
        }

        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        async Task<bool> CreateTiles(Action<MemoryStream, Tile> onTileCreated, TileFormat format, int tileSize, int zoom, List<Tile>? customTiles = null)
        {
            if (!CheckUri())
                return false;

            abordWork = false;

            SetButtonsState(false);

            var finalFormat = format == TileFormat.png ? CoreWebView2CapturePreviewImageFormat.Png : CoreWebView2CapturePreviewImageFormat.Jpeg;

            progressBar1.Value = 0;
            var tiles = new List<Tile>();
            if (customTiles == null)
            {
                var viewInfos = await GetTilesInfosForCurrentView(zoom);
                tiles = viewInfos.tiles;
            }
            else
            {
                tiles = customTiles;
            }

            var tileWorks = new List<List<Tile>>();
            var tilesCount = tiles.Count;
            var workCount = (int)threadCount.Value;
            if (workCount > 1)
            {
                if (tilesCount < 5)
                    workCount = 1;
                if (tilesCount < 10)
                    workCount = 2;
            }
            var workTileCount = tilesCount / workCount;
            progressBar1.Maximum = tiles.Count;
            while (tiles.Count > 0)
            {
                var tilesToTake = workTileCount;
                if (tiles.Count < tilesToTake)
                    tilesToTake = tiles.Count;
                tileWorks.Add(tiles.GetRange(0, tilesToTake));
                tiles.RemoveRange(0, tilesToTake);
            }

            webView21.Visible = false;
            foreach (var tileWork in tileWorks)
            {
                var newFlow = new FlowLayoutPanel();
                newFlow.FlowDirection = FlowDirection.LeftToRight;
                newFlow.Size = new Size(tileSize, tileSize + 15);

                var newProgressBar = new System.Windows.Forms.ProgressBar();
                newProgressBar.Size = new Size(tileSize, 10);
                newProgressBar.Style = ProgressBarStyle.Continuous;

                var newWebView = new Microsoft.Web.WebView2.WinForms.WebView2();
                newWebView.Enabled = false;
                newWebView.Source = new Uri("https://www.strava.com/heatmap", UriKind.Absolute);
                newWebView.Size = new Size(tileSize, tileSize);

                newFlow.Controls.Add(newWebView);
                newFlow.Controls.Add(newProgressBar);

                flowLayoutPanel1.Controls.Add(newFlow);

                newWebView.NavigationCompleted += async (sender, args) =>
                {
                    await Task.Delay(1000);

                    switch (mapStyle.SelectedIndex)
                    {
                        case 0: // none
                            await newWebView.ExecuteScriptAsync("document.getElementById('view-maps').click();");
                            break;
                        case 1: // dark
                            break;
                        case 2: // winter
                            await newWebView.ExecuteScriptAsync("document.querySelector('input[name=map_style][value=\"winter\"]').click();");
                            break;
                        case 3: // satellite
                            await newWebView.ExecuteScriptAsync("document.querySelector('input[name=map_style][value=\"satellite\"]').click();");
                            break;
                    }

                    await Task.Delay(1000);

                    switch (activityType.SelectedIndex)
                    {
                        case 0: // all
                            break;
                        case 1: // bikes
                            await newWebView.ExecuteScriptAsync("document.querySelector('input[name=map_type][value=\"ride\"]').click();");
                            break;
                        case 2: // runs
                            await newWebView.ExecuteScriptAsync("document.querySelector('input[name=map_type][value=\"run\"]').click();");
                            break;
                    }

                    WebView2DevToolsContext devToolsContext;
                    await semaphoreSlim.WaitAsync();
                    try
                    {
                        await Task.Delay(new Random().Next(100, 1000)); // Try to fix a random crash
                        devToolsContext = await newWebView.CoreWebView2.CreateDevToolsContextAsync();
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }

                    await PrepareMap(devToolsContext, newWebView, UICommand.Hide, tileSize, zoom);

                    newProgressBar.Maximum = tileWork.Count;
                    foreach (var tile in tileWork)
                    {
                        if (abordWork)
                            break;

                        await FitBounds(devToolsContext, newWebView, tile.west, tile.south, tile.east, tile.north);

                        MemoryStream memoryStream = new();
                        await newWebView.CoreWebView2.CapturePreviewAsync(finalFormat, memoryStream);
                        onTileCreated(memoryStream, tile);

                        memoryStream.Close();

                        progressBar1.Value++;
                        newProgressBar.Value++;
                    }

                    await devToolsContext.DisposeAsync();

                };

            }

            while (progressBar1.Value < progressBar1.Maximum)
            {
                if (abordWork)
                    goto end;
                await Task.Delay(100);
            }


        end:
            webView21.Visible = true;
            while (flowLayoutPanel1.Controls.Count > 1)
                flowLayoutPanel1.Controls.RemoveAt(1);
            SetButtonsState(true);
            return !abordWork;

        }

        async Task CreateAndSaveTile(TileFormat format, string folder, int tileSize, int zoom) // folder end with '\'
        {
            folder += zoom + @"\";

            var onTileCreated = delegate (MemoryStream stream, Tile tile)
            {
                Bitmap screenBitmap = new(stream);
                if (mapStyle.SelectedIndex == 0)
                    screenBitmap.MakeTransparent(Color.White);

                var relativePath = tile.x1 + @"\" + tile.y1 + ".png";
                var screenPath = folder + relativePath;

                var screenFileInfo = new FileInfo(screenPath);
                var screenFileDir = screenFileInfo.Directory;
                if (screenFileDir != null && !screenFileDir.Exists)
                    screenFileDir.Create();

                screenBitmap.Save(screenPath);
            };

            await CreateTiles(onTileCreated, format, tileSize, (int)tileZoom.Value);


        }

        private void NewWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }

        async private void createTiles_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select directory where to save tiles";

            var result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var folder = folderBrowserDialog.SelectedPath + @"\";
                await CreateAndSaveTile(TileFormat.png, folder, (int)tileSize.Value, (int)tileZoom.Value);
            }
        }

        private void createKarooTiles_Click(object sender, EventArgs e)
        {
            //FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            //folderBrowserDialog.Description = "Select folder with adb.exe";

            //var result = folderBrowserDialog.ShowDialog();
            //if (result == DialogResult.OK)
            //{
            //    var adbPath = folderBrowserDialog.SelectedPath + @"\adb.exe";
            //    var fileInfos = new FileInfo(adbPath);
            //    if (!fileInfos.Exists)
            //    {
            //        MessageBox.Show("Can't find adb.exe !");
            //        return;
            //    }

            //    var folder = Path.GetTempPath() + new Random().Next() + @"\";
            //    lastTempFolder = folder;
            //    await CreateAndSaveTile(TileFormat.png, folder, (int)tileSize.Value, (int)tileZoom.Value);

            //    Process process = new();
            //    ProcessStartInfo startInfo = new();
            //    //startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            //    startInfo.FileName = adbPath;
            //    folder += (int)tileZoom.Value + @"\";
            //    startInfo.Arguments = "push \"" + folder + ".\" /sdcard/offline/heatmap/" + (int)tileZoom.Value + "/";
            //    process.StartInfo = startInfo;
            //    process.Start();
            //    process.WaitForExit();

            //    if (Directory.Exists(lastTempFolder))
            //        Directory.Delete(lastTempFolder, true);
            //}
            new SendToAndroid().ShowDialog();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Directory.Exists(lastTempFolder))
                Directory.Delete(lastTempFolder, true);
        }

        bool abordWork = false;
        private void abord_Click(object sender, EventArgs e)
        {
            abordWork = true;
        }

        async private void updateTiles_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select tiles folder";

            var result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var tileDirInfos = new DirectoryInfo(folderBrowserDialog.SelectedPath);
                var tiles = new List<Tile>();
                foreach (var dirInfos in tileDirInfos.GetDirectories())
                {
                    int zoomNum = 0;
                    if (int.TryParse(dirInfos.Name, out zoomNum))
                    {
                        foreach (var subDirInfos in dirInfos.GetDirectories())
                        {
                            int xTile = 0;
                            if (int.TryParse(subDirInfos.Name, out xTile))
                            {
                                foreach (var file in subDirInfos.GetFiles("*.png"))
                                {
                                    int yTile = 0;
                                    if (int.TryParse(Path.GetFileNameWithoutExtension(file.Name), out yTile))
                                    {
                                        var newTile = new Tile(new Point(xTile, yTile), new Point(xTile + 1, yTile + 1), zoomNum);
                                        newTile.tag = file.FullName;
                                        tiles.Add(newTile);
                                    }
                                }
                            }
                        }
                    }
                }

                var onTileCreated = delegate (MemoryStream stream, Tile tile)
                {
                    Bitmap screenBitmap = new(stream);
                    if (mapStyle.SelectedIndex == 0)
                        screenBitmap.MakeTransparent(Color.White);

                    screenBitmap.Save(tile.tag);
                };

                await CreateTiles(onTileCreated, TileFormat.png, (int)tileSize.Value, (int)tileZoom.Value, tiles);

            }
        }
    }


}