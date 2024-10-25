﻿using OpenCvSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await CaptureVideo(true);
        }

        static async Task SendRequestAsync(string xmlFile)
        {
            string url = "http://192.168.100.19:8899/onvif/ptz";
            using (HttpClient client = new HttpClient())
            {
                string xmlContent = "";
                if (File.Exists(xmlFile))
                {
                    xmlContent = File.ReadAllText(xmlFile);
                }
                else
                {
                    Console.WriteLine($"File not found: {xmlFile}");
                }

                var content = new StringContent(xmlContent, System.Text.Encoding.UTF8, "text/xml");
                HttpResponseMessage response = await client.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Solicitud enviada con éxito.");
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                }
            }
        }

        static async Task CaptureVideo(bool saveToFile)
        {
            string rtspUrl = "rtsp://192.168.100.19/live/ch00_1";
            using (var capture = new VideoCapture(rtspUrl))
            {
                if (!capture.IsOpened())
                {
                    Console.WriteLine("No se pudo abrir la cámara.");
                    return;
                }

                Mat frame = new Mat();
                Cv2.NamedWindow("IonicAppCamera", WindowFlags.Normal);

                VideoWriter writer = null;
                if (saveToFile)
                {
                    Size frameSize = new Size(capture.FrameWidth, capture.FrameHeight);
                    writer = new VideoWriter("video.avi", FourCC.XVID, 30, frameSize);
                }

                bool isPtzCommandRunning = false;
                while (true)
                {
                    if (!capture.Read(frame) || frame.Empty())
                    {
                        Console.WriteLine("No se pudo capturar el frame.");
                        break;
                    }

                    Cv2.ImShow("IonicAppCamera", frame);

                    if (saveToFile)
                    {
                        writer?.Write(frame);
                    }

                    int key = Cv2.WaitKey(10);
                    if (HandleKeyPress(key, ref isPtzCommandRunning)) break;
                }

                writer?.Release();
                Cv2.DestroyAllWindows();
            }
        }

        static bool HandleKeyPress(int key, ref bool isPtzCommandRunning)
        {
            if (key == 105 && !isPtzCommandRunning) // 'i' for PTZ up
            {
                ExecutePtzCommandAsync(@"C:\path\to\postup.xml", isPtzCommandRunning);
            }
            else if (key == 44 && !isPtzCommandRunning) // ',' for PTZ down
            {
                ExecutePtzCommandAsync(@"C:\path\to\postdown.xml", isPtzCommandRunning);
            }
            else if (key == 27) // ESC to exit
            {
                Console.WriteLine("Saliendo...");
                return true;
            }
            return false;
        }

        static async Task ExecutePtzCommandAsync(string xmlFilePath, bool isPtzCommandRunning)
        {
            isPtzCommandRunning = true;
            await SendRequestAsync(xmlFilePath);
            isPtzCommandRunning = false;
        }
    }
}
