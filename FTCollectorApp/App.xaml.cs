﻿using FTCollectorApp.Model;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FTCollectorApp
{
    public partial class App : Application
    {
        public static string DatabaseLocation = string.Empty;
       
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new FTCollectorApp.MainPage()); // root page  is MainPage()
        }

        public App(string databaseLoc)
        {
            InitializeComponent();

            MainPage = new NavigationPage(new FTCollectorApp.MainPage());
            DatabaseLocation = databaseLoc;

        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
