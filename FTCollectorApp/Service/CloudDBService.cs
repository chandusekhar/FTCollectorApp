﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using System.Net.Http;
using Newtonsoft.Json;
using Plugin.Connectivity;
using FTCollectorApp.Model;
using Xamarin.Essentials;
using Xamarin.Forms;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using FTCollectorApp.Model.Reference;

namespace FTCollectorApp.Service
{
    public static class CloudDBService
    {
        static HttpClient client;
        static CloudDBService()
        {
            try
            {
                client = new HttpClient()
                {
                    BaseAddress = new Uri(Constants.BaseUrl)
                };
            }
            catch
            {

            }


        }


        // grab End User tables from Url https://collector.fibertrak.com/phonev4/xamarinLogin.php
        public static Task<IEnumerable<User>> GetEndUserFromAWSMySQLTable() =>
            GetAsync<IEnumerable<User>>(Constants.GetEndUserTableUrl);
        public static Task<IEnumerable<Job>> GetJobFromAWSMySQLTable() =>
            GetAsync<IEnumerable<Job>>(Constants.GetJobTableUrl);

        public static Task<IEnumerable<Site>> GetSiteFromAWSMySQLTable() =>
            GetAsync<IEnumerable<Site>>(Constants.GetSiteTableUrl);

        public static Task<IEnumerable<Crewdefault>> GetCrewDefaultFromAWSMySQLTable() =>
            GetAsync<IEnumerable<Crewdefault>>(Constants.GetCrewdefaultTableUrl);

        public static Task<IEnumerable<CodeSiteType>> GetCodeSiteTypeFromAWSMySQLTable() =>
            GetAsync<IEnumerable<CodeSiteType>>(Constants.GetCodeSiteTypeTableUrl);


        async static Task<T> GetAsync<T>(String Url)
        {
            var json = string.Empty;

            try
            {
                json = await client.GetStringAsync(Url);
                Console.WriteLine($"[CloudDBService] response : {json}");
                var content = JsonConvert.DeserializeObject<T>(json);
                //var sqliteContent = JsonConvert.DeserializeObject<List<User>>(response);

                Console.WriteLine($"[CloudDBService] content : {content.ToString()}");
                return content;
            }
            catch (Exception exp)
            {
                Console.WriteLine("Exception {0}", exp.ToString());
            }

            return JsonConvert.DeserializeObject<T>(json);
        }
        public static async Task PostJobEvent() => await PostJobEvent("", "","");
        public static async Task PostJobEvent(string odo) => await PostJobEvent("", odo,"");
        public static async Task PostJobEvent(string param1, string param2, string perDiem)
        {

            var keyValues = new List<KeyValuePair<string, string>>{
                new KeyValuePair<string, string>("jobnum",Session.jobnum),
                new KeyValuePair<string, string>("uid", Session.uid.ToString()),

                new KeyValuePair<string, string>("min", Session.event_type == Session.ClockIn ? param1 : "0"),
                new KeyValuePair<string, string>("hr", Session.event_type == Session.ClockIn ? param2 : "0"),

                new KeyValuePair<string, string>("perdiem",perDiem),
                new KeyValuePair<string, string>("gps_sts", Session.gps_sts),

                // xSaveJobEvents.php Line 59 : $longitude=$_POST['longitude2'];
                // xSaveJobEvents.php Line 60 : $latitude =$_POST['lattitude2'];
                new KeyValuePair<string, string>("manual_latti", Session.gps_sts == "1" ? "0":Session.manual_latti),
                new KeyValuePair<string, string>("manual_longi", Session.gps_sts == "1" ? "0":Session.manual_longi),

                // xSaveJobEvents.php Line 73 : $longitude=$_POST['longitude2'];
                // xSaveJobEvents.php Line 74 : $latitude =$_POST['lattitude2'];
                new KeyValuePair<string, string>("lattitude2", Session.lattitude2),
                new KeyValuePair<string, string>("longitude2", Session.longitude2),
                new KeyValuePair<string, string>("odometer", Session.event_type),
                new KeyValuePair<string, string>("evtype", Session.event_type),
                new KeyValuePair<string, string>("odometer", param2.ToString()), // only for sending odometer 
                
                new KeyValuePair<string, string>("time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),

                new KeyValuePair<string, string>("ajaxname", Constants.InsertJobEvents)
            };
            // this Httpconten will work for Content-type : x-wwww-url-formencoded REST
            HttpContent content = new FormUrlEncodedContent(keyValues);

            HttpResponseMessage response = null;

            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                response = await client.PostAsync(Constants.InsertJobEvents, content);
                if (response.IsSuccessStatusCode)
                {
                    var isi = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[CloudService] Response from {Constants.InsertJobEvents} OK = 200 , content :" + isi);
                }
            }
            else
            {
                // Put to Pending Sync
                var app = Application.Current as App;
                app.TaskCount += 1;


                keyValues.Add(new KeyValuePair<string, string>("Status", "Pending"));

                // put keyvaluepair to App properties as Hash<taskid, string keyvaluepair> with json 
                // store 
                // app.Properties[$"Task-{app.TaskCount}"] = JsonConvert.SerializeObject(keyValues);
                // var storedPendingTaskName = app.PendingTask;
                // List<string> tasklist = JsonConvert.DeserializeObject(storedPendingTaskName);


                // Serialize 
                var test = new Dictionary<string, List<KeyValuePair<string, string>>>();
                test.Add($"Task-{app.TaskCount}", keyValues);

                // To serialize the hashtable and its key/value pairs,
                // you must first open a stream for writing.
                // In this case, use a file stream.
                FileStream fs = new FileStream("PendingTaskFile.dat", FileMode.Append);

                // Construct a BinaryFormatter and use it to serialize the data to the stream.
                BinaryFormatter formatter = new BinaryFormatter();
                try
                {
                    formatter.Serialize(fs, test);
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                    throw;
                }
                finally
                {
                    fs.Close();
                }

            }
        }

        public static async Task PostPendingTask(string pendingTaskKey)
        {
            //Deserialize
            var DkeyValues = new Dictionary<string, List<KeyValuePair<string, string>>>();

            // Open the file containing the data that you want to deserialize.
            using (FileStream fs = new FileStream("PendingTaskFile.dat", FileMode.Open))
            { 
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();

                    // Deserialize the hashtable from the file and
                    // assign the reference to the local variable.
                    DkeyValues = (Dictionary<string, List<KeyValuePair<string, string>>>)formatter.Deserialize(fs);

                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    throw;
                }
            }

            // To prove that the table deserialized correctly,
            // display the key/value pairs.
            try
            {
                HttpContent content = new FormUrlEncodedContent(DkeyValues[pendingTaskKey]);

                HttpResponseMessage response = null;

                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    response = await client.PostAsync(Constants.CreateSiteTableUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var isi = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[CloudService.PostSiteAsync] Response from  OK = 200 , content :" + isi);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
            }

        }


        public static List<KeyValuePair<string, string>> PrepareContentData(string tagnum, string typecode)
        {
            var keyValues = new List<KeyValuePair<string, string>>{
                //new KeyValuePair<string, string>("jobnum",Session.jobnum),
                new KeyValuePair<string, string>("jno",Session.jobnum),
                new KeyValuePair<string, string>("uid", Session.uid.ToString()),
                new KeyValuePair<string, string>("tag",tagnum),
                new KeyValuePair<string, string>("typecode",typecode),
                new KeyValuePair<string, string>("plansheet","0"),
                new KeyValuePair<string, string>("psitem","0"),


                //new KeyValuePair<string, string>("gps_sts", Session.gps_sts),                
                //new KeyValuePair<string, string>("manual_latti", Session.manual_latti),
                //new KeyValuePair<string, string>("manual_longi", Session.manual_longi),

                new KeyValuePair<string, string>("lattitude2", Session.lattitude2),
                new KeyValuePair<string, string>("longitude2", Session.longitude2),
                new KeyValuePair<string, string>("altitude", Session.altitude),
                new KeyValuePair<string, string>("accuracy", Session.accuracy),

                //new KeyValuePair<string, string>("evtype", Session.event_type),
                
                new KeyValuePair<string, string>("time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),  // created_on
                new KeyValuePair<string, string>("owner", Session.ownerkey), //
                new KeyValuePair<string, string>("user", Session.uid.ToString()),
                new KeyValuePair<string, string>("stage", Session.stage),
                new KeyValuePair<string, string>("gpstime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                new KeyValuePair<string, string>("ownerCD", Session.ownerCD),
                new KeyValuePair<string, string>("ownerkey", Session.ownerkey),
                new KeyValuePair<string, string>("jobkey", Session.jobkey),
                new KeyValuePair<string, string>("createdfrm", "field collection"),
                new KeyValuePair<string, string>("usercounty", Session.countycode),
                new KeyValuePair<string, string>("ajaxname", Constants.CreateSiteTableUrl),


            };
            return keyValues;
        }


        public static async Task PostCreateSiteAsync(string tagnum, string typecode)
        {


            var keyValues = new List<KeyValuePair<string, string>>{
                //new KeyValuePair<string, string>("jobnum",Session.jobnum),
                new KeyValuePair<string, string>("jno",Session.jobnum),
                new KeyValuePair<string, string>("uid", Session.uid.ToString()),
                new KeyValuePair<string, string>("tag",tagnum),
                new KeyValuePair<string, string>("typecode",typecode),
                new KeyValuePair<string, string>("plansheet","0"),
                new KeyValuePair<string, string>("psitem","0"),


                //new KeyValuePair<string, string>("gps_sts", Session.gps_sts),                
                //new KeyValuePair<string, string>("manual_latti", Session.manual_latti),
                //new KeyValuePair<string, string>("manual_longi", Session.manual_longi),

                new KeyValuePair<string, string>("lattitude2", Session.lattitude2),
                new KeyValuePair<string, string>("longitude2", Session.longitude2),
                new KeyValuePair<string, string>("altitude", Session.altitude),
                new KeyValuePair<string, string>("accuracy", Session.accuracy),

                //new KeyValuePair<string, string>("evtype", Session.event_type),
                
                new KeyValuePair<string, string>("time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),  // created_on
                new KeyValuePair<string, string>("owner", Session.ownerkey), //
                new KeyValuePair<string, string>("user", Session.uid.ToString()),
                new KeyValuePair<string, string>("stage", Session.stage),
                new KeyValuePair<string, string>("gpstime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                new KeyValuePair<string, string>("ownerCD", Session.ownerCD),
                new KeyValuePair<string, string>("ownerkey", Session.ownerkey),
                new KeyValuePair<string, string>("jobkey", Session.jobkey),
                new KeyValuePair<string, string>("createdfrm", "field collection"),
                new KeyValuePair<string, string>("usercounty", Session.countycode),
                new KeyValuePair<string, string>("ajaxname", Constants.CreateSiteTableUrl),


            };
            // this Httpconten will work for Content-type : x-wwww-url-formencoded REST
            HttpContent content = new FormUrlEncodedContent(keyValues);

            HttpResponseMessage response = null;

            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                response = await client.PostAsync(Constants.CreateSiteTableUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var isi = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[CloudService.PostSiteAsync] Response from  OK = 200 , content :" + isi);
                }
            }
            else
            {
                // Put to Pending Sync
                var app = Application.Current as App;
                app.TaskCount += 1;
                keyValues.Add(new KeyValuePair<string, string>("Status", "Pending"));


                // Serialize 
                var test = new Dictionary<string, List<KeyValuePair<string, string>>>();
                test.Add($"Task-{app.TaskCount}", keyValues);


                // To serialize the hashtable and its key/value pairs,
                // you must first open a stream for writing.
                // In this case, use a file stream.
                using (FileStream fs = new FileStream(App.InternalStorageLocation, FileMode.Append, FileAccess.Write))
                {
                    // Construct a BinaryFormatter and use it to serialize the data to the stream.
                    BinaryFormatter formatter = new BinaryFormatter();
                    try
                    {
                        formatter.Serialize(fs, test);
                    }
                    catch (SerializationException e)
                    {
                        Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                        throw;
                    }
                }


            }
        }




        public static async Task SaveCrewdata(string OWNER_CD, string name1, string name2, string name3, string name4, string name5, string name6, string diem1, string diem2, string diem3, string diem4, string diem5, string diem6, string driver11, string driver12, string driver13, string driver14, string driver15, string driver16)
        {

            var keyValues = new List<KeyValuePair<string, string>>{
                new KeyValuePair<string, string>("evtype",Session.CrewAssembled),
                new KeyValuePair<string, string>("time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                new KeyValuePair<string, string>("jobnum", Session.jobnum),
                new KeyValuePair<string, string>("uid", Session.uid.ToString()),
                new KeyValuePair<string, string>("OWNER_CD", OWNER_CD),
                new KeyValuePair<string, string>("name1", name1),
                new KeyValuePair<string, string>("name2", name2),
                new KeyValuePair<string, string>("name3", name3),
                new KeyValuePair<string, string>("name4", name4),
                new KeyValuePair<string, string>("name5", name5),
                new KeyValuePair<string, string>("name6", name6),
                new KeyValuePair<string, string>("diem1", diem1),
                new KeyValuePair<string, string>("diem2", diem2),
                new KeyValuePair<string, string>("diem3", diem3),
                new KeyValuePair<string, string>("diem4", diem4),
                new KeyValuePair<string, string>("diem5", diem5),
                new KeyValuePair<string, string>("diem6", diem6),
                new KeyValuePair<string, string>("driver11", driver11),
                new KeyValuePair<string, string>("driver12", driver12),
                new KeyValuePair<string, string>("driver13", driver13),
                new KeyValuePair<string, string>("driver14", driver14),
                new KeyValuePair<string, string>("driver15", driver15),
                new KeyValuePair<string, string>("driver16", driver16),
                new KeyValuePair<string, string>("lattitude", Session.lattitude2),
                new KeyValuePair<string, string>("longitude", Session.longitude2)
            };
            // this Httpconten will work for Content-type : x-wwww-url-formencoded REST
            HttpContent content = new FormUrlEncodedContent(keyValues);

            HttpResponseMessage response = null;

            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                response = await client.PostAsync(Constants.SaveCrewUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var isi = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[CloudService.SaveCrewdata] Response OK = 200 , content :" + isi);
                }
            }
            else
            {
                // Put to Pending Sync
                // Put to Pending Sync
                var app = Application.Current as App;
                app.TaskCount += 1;
                keyValues.Add(new KeyValuePair<string, string>("Status", "Pending"));

                // Serialize 
                var test = new Dictionary<string, List<KeyValuePair<string, string>>>();
                test.Add($"Task-{app.TaskCount}", keyValues);

                // To serialize the hashtable and its key/value pairs,
                // you must first open a stream for writing.
                // In this case, use a file stream.
                using (FileStream fs = new FileStream(App.InternalStorageLocation, FileMode.Append, FileAccess.Write))
                {
                    // Construct a BinaryFormatter and use it to serialize the data to the stream.
                    BinaryFormatter formatter = new BinaryFormatter();
                    try
                    {
                        formatter.Serialize(fs, test);
                    }
                    catch (SerializationException e)
                    {
                        Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                        throw;
                    }
                }
            }
        }


        public static Task<IEnumerable<Manufacturer>> GetManufacturerTable() =>
            GetDropDownParamsAsync<IEnumerable<Manufacturer>>("manufacturer_list");
        public static Task<IEnumerable<JobSubmittal>> GetJobSubmittalTable() =>
            GetDropDownParamsAsync<IEnumerable<JobSubmittal>>("job_submittal");

        public static Task<IEnumerable<KeyType>> GetKeyTypeTable() =>
            GetDropDownParamsAsync<IEnumerable<KeyType>>("keytype");
        public static Task<IEnumerable<MaterialCode>> GetMaterialCodeTable() =>
            GetDropDownParamsAsync<IEnumerable<MaterialCode>>("materialcode");

        public static Task<IEnumerable<Mounting>> GetMountingTable() =>
            GetDropDownParamsAsync<IEnumerable<Mounting>>("mounting");

        public static Task<IEnumerable<FilterType>> GetFilterType() =>
            GetDropDownParamsAsync<IEnumerable<FilterType>>("filter_type");

        public static Task<IEnumerable<Roadway>> GetRoadway() =>
            GetDropDownParamsAsync<IEnumerable<Roadway>>("roadway");

        public static Task<IEnumerable<OwnerRoadway>> GetOwnerRoadway() =>
            GetDropDownParamsAsync<IEnumerable<OwnerRoadway>>("owner_roadway");

        public static Task<IEnumerable<InterSectionRoad>> GetIntersection() =>
            GetDropDownParamsAsync<IEnumerable<InterSectionRoad>>("intersection");

        public static Task<IEnumerable<ElectricCircuit>> GetElectricCircuit() =>
            GetDropDownParamsAsync<IEnumerable<ElectricCircuit>>("electric");

        public static Task<IEnumerable<Direction>> GetDirection() =>
            GetDropDownParamsAsync<IEnumerable<Direction>>("direction");
        public static Task<IEnumerable<DuctSize>> GetDuctSize() =>
            GetDropDownParamsAsync<IEnumerable<DuctSize>>("dsize");
        public static Task<IEnumerable<DuctType>> GetDuctType() =>
            GetDropDownParamsAsync<IEnumerable<DuctType>>("ducttype");
        public static Task<IEnumerable<GroupType>> GetGroupType() =>
            GetDropDownParamsAsync<IEnumerable<GroupType>>("grouptype");


        public static Task<IEnumerable<DevType>> GetDevType() =>
            GetDropDownParamsAsync<IEnumerable<DevType>>("devtype");

        public static Task<IEnumerable<RackNumber>> GetRackNumber() =>
            GetDropDownParamsAsync<IEnumerable<RackNumber>>("racknumber");

        public static Task<IEnumerable<RackType>> GetRackType() =>
            GetDropDownParamsAsync<IEnumerable<RackType>>("racktype");
        //
        public static Task<IEnumerable<Sheath>> GetSheath() =>
            GetDropDownParamsAsync<IEnumerable<Sheath>>("sheath");
        public static Task<IEnumerable<ReelId>> GetReelId() =>
            GetDropDownParamsAsync<IEnumerable<ReelId>>("reelid");

        public static Task<IEnumerable<Orientation>> GetOrientation() =>
            GetDropDownParamsAsync<IEnumerable<Orientation>>("sbto");

        public static Task<IEnumerable<Chassis>> GetChassis() =>
            GetDropDownParamsAsync<IEnumerable<Chassis>>("toencloser");
        public static Task<IEnumerable<AFiberCable>> GetAFCable() =>
            GetDropDownParamsAsync<IEnumerable<AFiberCable>>("frcable");
        public static Task<IEnumerable<CableType>> GetCableType() =>
            GetDropDownParamsAsync<IEnumerable<CableType>>("cable_type");
        public static Task<IEnumerable<EquipmentType>> GetEquipmentType() =>
            GetDropDownParamsAsync<IEnumerable<EquipmentType>>("equipment_code");
        public static Task<IEnumerable<EquipmentDetailType>> GetEquipmentDetail() =>
            GetDropDownParamsAsync<IEnumerable<EquipmentDetailType>>("equipment_detail");
        public static Task<IEnumerable<CableStructure>> GetCableStructure() =>
            GetDropDownParamsAsync<IEnumerable<CableStructure>>("cable_structure");

        ///
        //public static Task<IEnumerable<Side>> GetSide() =>
        //   GetBuildingDropDownParamsAsync<IEnumerable<Side>>("side");
        public static Task<IEnumerable<Tracewaretag>> GetTraceWareTag() =>
           GetDropDownParamsAsync<IEnumerable<Tracewaretag>>("tracewaretag");

        //public static Task<IEnumerable<Cable>> GetCables() =>
        //   GetBuildingDropDownParamsAsync<IEnumerable<Cable>>("cables");
        public static Task<IEnumerable<ConduitsGroup>> GetConduits() =>
            GetDropDownParamsAsync<IEnumerable<ConduitsGroup>>("conduits");
        public static Task<IEnumerable<Owner>> GetOwners() =>
           GetDropDownParamsAsync<IEnumerable<Owner>>("owners");
        public static Task<IEnumerable<County>> GetAllCountry() =>
           GetDropDownParamsAsync<IEnumerable<County>>("allcountry");
        public static Task<IEnumerable<InstallType>> GetInstallType() =>
           GetDropDownParamsAsync<IEnumerable<InstallType>>("installtype");

        public static Task<IEnumerable<DuctUsed>> GetDuctUsed() =>
           GetDropDownParamsAsync<IEnumerable<DuctUsed>>("ductused");

        //////
        public static Task<IEnumerable<FilterSize>> GetFilterSize() =>
            GetDropDownParamsAsync<IEnumerable<FilterSize>>("fltrsizes");

        public static Task<IEnumerable<SpliceType>> GetSpliceType() =>
            GetDropDownParamsAsync<IEnumerable<SpliceType>>("splicetype");

        public static Task<IEnumerable<Chassis>> GetToEncloser() =>
            GetDropDownParamsAsync<IEnumerable<Chassis>>("toencloser");

        public static Task<IEnumerable<LaborClass>> GetLaborClass() =>
            GetDropDownParamsAsync<IEnumerable<LaborClass>>("laborclass");

        public static Task<IEnumerable<Dimensions>> GetDimensions() =>
            GetDropDownParamsAsync<IEnumerable<Dimensions>>("dimensions");

        public static Task<IEnumerable<CompassDirection>> GetCompassDir() =>
            GetDropDownParamsAsync<IEnumerable<CompassDirection>>("travellen");

        public static Task<IEnumerable<BuildingType>> GetBuildingType() =>
            GetDropDownParamsAsync<IEnumerable<BuildingType>>("bClassification");
        async static Task<T> GetDropDownParamsAsync<T>(string type)
        {
            var keyValues = new List<KeyValuePair<string, string>>{
                new KeyValuePair<string, string>("type",type),
            };

            var json = String.Empty;
            try
            {
                HttpContent content = new FormUrlEncodedContent(keyValues);

                HttpResponseMessage response = null;

                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    response = await client.PostAsync(Constants.GetBuildingsParamUrl, content);

                    if (response.IsSuccessStatusCode)
                    {

                        json = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[CloudService.{type}] Response from  OK = 200 , content :" + json);
                        var data = JsonConvert.DeserializeObject<T>(json);
                        return data;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
            }
            return JsonConvert.DeserializeObject<T>(json);
        }


        
        public static async Task PostSaveFiberOpticCable(List<KeyValuePair<string, string>> keyValues)
        {

            // this Httpconten will work for Content-type : x-wwww-url-formencoded REST
            HttpContent content = new FormUrlEncodedContent(keyValues);
            var json = JsonConvert.SerializeObject(keyValues);
            Console.WriteLine($"PostSaveBuilding Json : {json}");
            HttpResponseMessage response = null; 

            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                response = await client.PostAsync(Constants.UpdateAfiberCableTableUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var isi = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[PostSaveBuilding] Response from  OK = 200 , content :" + isi);
                }
            }
            else
            {
                // Put to Pending Sync
                var app = Application.Current as App;
                app.TaskCount += 1;
                keyValues.Add(new KeyValuePair<string, string>("Status", "Pending"));


                // Serialize 
                var test = new Dictionary<string, List<KeyValuePair<string, string>>>();
                test.Add($"Task-{app.TaskCount}", keyValues);


                // To serialize the hashtable and its key/value pairs,
                // you must first open a stream for writing.
                // In this case, use a file stream.
                using (FileStream fs = new FileStream(App.InternalStorageLocation, FileMode.Append, FileAccess.Write))
                {
                    // Construct a BinaryFormatter and use it to serialize the data to the stream.
                    BinaryFormatter formatter = new BinaryFormatter();
                    try
                    {
                        formatter.Serialize(fs, test);
                    }
                    catch (SerializationException e)
                    {
                        Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                        throw;
                    }
                }


            }
        }
        public static async Task PostSaveBuilding(List<KeyValuePair<string, string>> keyValues)
        {

            // this Httpconten will work for Content-type : x-wwww-url-formencoded REST
            HttpContent content = new FormUrlEncodedContent(keyValues);
            var json = JsonConvert.SerializeObject(keyValues);
            Console.WriteLine($"PostSaveBuilding Json : {json}");
            HttpResponseMessage response = null;

            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                response = await client.PostAsync(Constants.UpdateSiteTableUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var isi = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[PostSaveBuilding] Response from  OK = 200 , content :" + isi);
                }
            }
            else
            {
                // Put to Pending Sync
                var app = Application.Current as App;
                app.TaskCount += 1;
                keyValues.Add(new KeyValuePair<string, string>("Status", "Pending"));


                // Serialize 
                var test = new Dictionary<string, List<KeyValuePair<string, string>>>();
                test.Add($"Task-{app.TaskCount}", keyValues);


                // To serialize the hashtable and its key/value pairs,
                // you must first open a stream for writing.
                // In this case, use a file stream.
                using (FileStream fs = new FileStream(App.InternalStorageLocation, FileMode.Append, FileAccess.Write))
                {
                    // Construct a BinaryFormatter and use it to serialize the data to the stream.
                    BinaryFormatter formatter = new BinaryFormatter();
                    try
                    {
                        formatter.Serialize(fs, test);
                    }
                    catch (SerializationException e)
                    {
                        Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                        throw;
                    }
                }


            }
        }
    }





}



