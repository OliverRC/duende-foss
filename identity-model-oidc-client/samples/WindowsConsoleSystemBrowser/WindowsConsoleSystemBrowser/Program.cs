﻿using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace WindowsConsoleSystemBrowser
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Any())
            {
                await ProcessCallback(args[0]);
            }
            else
            {
                await Run();
            }
        }

        private static async Task ProcessCallback(string args)
        {
            var response = new AuthorizeResponse(args);
            if (!String.IsNullOrWhiteSpace(response.State))
            {
                Console.WriteLine($"Found state: {response.State}");
                var callbackManager = new CallbackManager(response.State);
                await callbackManager.RunClient(args);
            }
            else
            {
                Console.WriteLine("Error: no state on response");
            }
        }

        const string CustomUriScheme = "sample-windows-client";

        static async Task Run()
        {
            new RegistryConfig(CustomUriScheme).Configure();

            Console.WriteLine("+-----------------------+");
            Console.WriteLine("|  Sign in with OIDC    |");
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("");
            Console.WriteLine("Press any key to sign in...");
            Console.ReadKey();

            Program p = new Program();
            await p.SignIn();

            Console.ReadKey();
        }

        private async Task SignIn()
        {
            // create a redirect URI using the custom redirect uri
            string redirectUri = string.Format(CustomUriScheme + "://callback");
            Console.WriteLine("redirect URI: " + redirectUri);

            var options = new OidcClientOptions
            {
                Authority = "https://demo.duendesoftware.com",
                ClientId = "interactive.public",
                Scope = "openid profile api",
                RedirectUri = redirectUri,
            };

            var client = new OidcClient(options);
            var state = await client.PrepareLoginAsync();

            Console.WriteLine($"Start URL: {state.StartUrl}");

            var callbackManager = new CallbackManager(state.State);

            // open system browser to start authentication
            Process.Start(new ProcessStartInfo
            {
                FileName = state.StartUrl,
                UseShellExecute = true
            });

            Console.WriteLine("Running callback manager");
            var response = await callbackManager.RunServer();

            Console.WriteLine($"Response from authorize endpoint: {response}");

            // Brings the Console to Focus.
            BringConsoleToFront();

            var result = await client.ProcessResponseAsync(response, state);

            BringConsoleToFront();

            if (result.IsError)
            {
                Console.WriteLine("\n\nError:\n{0}", result.Error);
            }
            else
            {
                Console.WriteLine("\n\nClaims:");
                foreach (var claim in result.User.Claims)
                {
                    Console.WriteLine("{0}: {1}", claim.Type, claim.Value);
                }

                Console.WriteLine();
                Console.WriteLine("Access token:\n{0}", result.AccessToken);

                if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                {
                    Console.WriteLine("Refresh token:\n{0}", result.RefreshToken);
                }
            }
        }

        // Hack to bring the Console window to front.
        // ref: http://stackoverflow.com/a/12066376
        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public void BringConsoleToFront()
        {
            SetForegroundWindow(GetConsoleWindow());
        }
    }
}