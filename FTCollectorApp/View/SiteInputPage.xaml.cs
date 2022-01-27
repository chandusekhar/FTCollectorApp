﻿using FTCollectorApp.Model;
using FTCollectorApp.Service;
using Plugin.Connectivity;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FTCollectorApp.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SiteInputPage : ContentPage
    {

        private ObservableCollection<CodeSiteType> CodeSiteTypes = new ObservableCollection<CodeSiteType>();
        private ObservableCollection<Site> Sites = new ObservableCollection<Site>();
        private ObservableCollection<string> TagNumbers;

        private bool _isBusy;

        public bool isBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy == value)
                    return;
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        private bool _stage;


        public SiteInputPage()
        {
            InitializeComponent();
            BindingContext = this;
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            IsBusy = true;
            Console.WriteLine("Connection : " + Connectivity.NetworkAccess.ToString());

            CrossConnectivity.Current.ConnectivityChanged += OnConnectivityHandler;

            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {

                CodeSiteTypes.Clear();
                Sites.Clear();

                var contentCodeSiteType = await CloudDBService.GetCodeSiteTypeFromAWSMySQLTable();

                CodeSiteTypes = new ObservableCollection<CodeSiteType>(contentCodeSiteType);
                Console.WriteLine(contentCodeSiteType);


                var contentSite = await CloudDBService.GetSiteFromAWSMySQLTable();

                Sites = new ObservableCollection<Site>(contentSite);
                Console.WriteLine(contentSite);

                // push code_site_type Tables to local SQLite. Model is in Model.Job
                // with using(SQLiteConnection) we didn't have to do conn.close()
                using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
                {
                    conn.CreateTable<CodeSiteType>();
                    conn.InsertAll(contentCodeSiteType);

                    conn.CreateTable<Site>();
                    conn.InsertAll(contentSite);
                }
            }
            else
            {
                // because no internet network
                // Read Job Tables to local SQLite. Model is in Model.Job
                // with using(SQLiteConnection) we didn't have to do conn.close()
                using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
                {
                    conn.CreateTable<CodeSiteType>();
                    var codesiteypes = conn.Table<CodeSiteType>().ToList();
                    CodeSiteTypes = new ObservableCollection<CodeSiteType>(codesiteypes);
                    //listviewPost.ItemsSource = listPost; //= new ObservableCollection<Post>(posts);

                }
            }



            // select MajorSite type from Sites (LINQ command)
            //var majorSites = CodeSiteTypes.GroupBy(b => b.MajorSites).Select(g => g.First()).ToList();
            //foreach (var majorSite in majorSites)
            //    majorSitePicker.Items.Add(majorSite.MajorSites);
            var majorTypes = CodeSiteTypes.GroupBy(b => b.MajorType).Select(g => g.First()).ToList();
            foreach (var majorType in majorTypes)
                majorTypePicker.Items.Add(majorType.MajorType);


            List<string> tagnumbers = new List<string>();
            var tagNumbers = Sites.GroupBy(b => b.TagNumber).Select(g => g.First()).ToList();
            foreach (var tagNumber in tagNumbers) {
                tagnumbers.Add(tagNumber.TagNumber);
                Console.WriteLine($"tagnumber : {tagNumber.TagNumber}");
            }
            TagNumbers = new ObservableCollection<string>(tagnumbers);

            //await LocateService.GetLocation(); // get current location
            //await PopupNavigation.Instance.PushAsync(new GpsDevicePopUpView()); // for Rg.plugin popup
            
            stagePicker.Items.Add(Session.stage);

            IsBusy = false;
        }

        private async void OnConnectivityHandler(object sender, Plugin.Connectivity.Abstractions.ConnectivityChangedEventArgs e)
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                CodeSiteTypes.Clear();
                Sites.Clear();

                var contentCodeSiteType = await CloudDBService.GetCodeSiteTypeFromAWSMySQLTable();

                CodeSiteTypes = new ObservableCollection<CodeSiteType>(contentCodeSiteType);
                Console.WriteLine(contentCodeSiteType);


                var contentSite = await CloudDBService.GetSiteFromAWSMySQLTable();

                Sites = new ObservableCollection<Site>(contentSite);
                Console.WriteLine(contentSite);

                // push code_site_type Tables to local SQLite. Model is in Model.Job
                // with using(SQLiteConnection) we didn't have to do conn.close()
                using (SQLiteConnection conn = new SQLiteConnection(App.DatabaseLocation))
                {
                    conn.CreateTable<CodeSiteType>();
                    conn.InsertAll(contentCodeSiteType);

                    conn.CreateTable<Site>();
                    conn.InsertAll(contentSite);
                }
            }
        }

        private void btnRecordGPS_Clicked(object sender, EventArgs e)
        {

        }

        private void btnGPSOffset_Clicked(object sender, EventArgs e)
        {

        }

        private void majorTypeP_SelectedIdxChanged(object sender, EventArgs e)
        {
            var selectedMajorType = majorTypePicker.Items[majorTypePicker.SelectedIndex];

            var minorTypes = CodeSiteTypes.Where(a => a.MajorType == selectedMajorType).GroupBy(b => b.MinorType).Select(g => g.First()).ToList();
            foreach (var minorType in minorTypes)
                minorTypePicker.Items.Add(minorType.MinorType);
        }

        private void minorTypeP_SelectedIdxChanged(object sender, EventArgs e)
        {

            var selectedMinorType = minorTypePicker.Items[minorTypePicker.SelectedIndex];
            var selectedMajorType = majorTypePicker.Items[majorTypePicker.SelectedIndex];

        }

        private async void entryTagNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TagNumbers.Contains(e.NewTextValue))
            {
                await DisplayAlert("Waring", $"Tag {e.NewTextValue} already taken", "OK");
                entryTagNum.Text = "";
            }
        }

        private void entryTagNum2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(entryTagNum.Equals(entryTagNum2))
            {
                stsReEnter.Text = "Match!";
                btnRecordGPS.IsEnabled = true;
                btnGPSOffset.IsEnabled = true;
            }
            else
            {
                stsReEnter.Text = "Check Again Tag";
                btnRecordGPS.IsEnabled = false;
                btnGPSOffset.IsEnabled = false;
            }
        }
    }
}