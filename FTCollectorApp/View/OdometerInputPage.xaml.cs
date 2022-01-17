﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FTCollectorApp.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OdometerInputPage : ContentPage
    {
        public OdometerInputPage()
        {
            InitializeComponent();
        }

        private void btnFinish_Clicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }

        private void btnLogOut_Clicked(object sender, EventArgs e)
        {
            Navigation.PopToRootAsync();
        }
    }
}