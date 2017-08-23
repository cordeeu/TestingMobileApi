﻿using System;
using System.Collections.Generic;
using System.Web.Http;
using MobileApi.Models;
using MobileApi.Helpers;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Linq;
using System.Web.Http.OData;
using System.Data;
using System.Data.OleDb;
using System.Web.Script.Serialization;
using System.Web;
using System.Data.Entity;
using Ionic.Zip;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace MobileApi.Controllers
{ 

    public class BaseController : ApiController
    {
        public WoodyPlantsMobileApiContext woodyDb = new WoodyPlantsMobileApiContext();
        public WetlandPlantsMobileApiContext wetlandDb = new WetlandPlantsMobileApiContext();
        public string currentRepository;

        [EnableQuery]
        [Route("api/woody")] 
        public IQueryable<WoodyPlant> GetWoodyPlants()
        {
            DbSet<WoodyPlant> allPlants = woodyDb.Plants;
            return allPlants.AsQueryable();
        }

        // This action accepts no params (other than the repository) and also supports OData querying params
        // e.g. api/wetland?$top=10 (gets top ten records)
        // e.g. api/wetland?$filter=commonname eq 'Acer' (returns pumas with name equaling 'Acer')
        [EnableQuery, AuthenticationTokenWetlands]
        [Route("api/wetland")]
        public IQueryable<WetlandPlant> GetWetlandPlants()
        {
            wetlandDb.Configuration.ProxyCreationEnabled = false;
            //DbSet<WetlandPlant> allPlants = wetlandDb.Plants;
            List<WetlandPlant> allPlants = wetlandDb.Plants.Include(x => x.Images).Include(x => x.References).Include(x => x.SimilarSpecies).ToList();
                //.Include(x=>x.SimilarSpeciesWetland).Include(x=>x.CountyPlantWetland)
                //.Include(x=>x.FruitWetland).Include(x=>x.DivisionWetland).Include(x=>x.ShapeWetland).Include(x=>x.ArrangementWetland)
                //.Include(x=>x.SizeWetland).Include(x=>x.RegionWetland).ToList();

            return allPlants.AsQueryable();
        }

        [AuthenticationTokenWetlands]
        [Route("api/wetland_settings/{settingName}")]
        public IHttpActionResult GetWetlandSetting(string settingName)
        {
            WetlandSetting setting = wetlandDb.WetlandSettings.Where(b => b.name == settingName).FirstOrDefault();
            if (setting == null)
            {
                return NotFound();
            }
            return Ok(setting);
        }

        [AuthenticationTokenWetlands]
        [Route("api/wetland_settings/images")]
        public List<WetlandSetting> GetWetlandImageZipFileSettings()
        {
            List<WetlandSetting> wetlandImageZipFileSettings = wetlandDb.WetlandSettings.Where(b => b.name == "ImagesZipFile").ToList();
            return wetlandImageZipFileSettings;
        }

        [AuthenticationTokenWetlands]
        [Route("api/wetland_glossary")]
        public List<WetlandGlossary> GetWetlandGlossary()
        {
            List<WetlandGlossary> wetlandGlossary = wetlandDb.WetlandGlossary.ToList();
            return wetlandGlossary;
        }

        /*   [Route("api/{repository}/{resourceId}")]
           public async Task<IHttpActionResult> Get(int resourceId)
           {
               Puma puma = await db.Pumas.FindAsync(resourceId);
               if (puma == null)
               {
                   return NotFound();
               }

               return Ok(puma);
           }
           */

        // Get zipped images
        [AuthenticationTokenWetlands]
        [Route("api/wetland/image_zip_files/{fileName}")]
           public HttpResponseMessage GetImagesZip(string fileName)
           {
                var directory = (Debugger.IsAttached == true) ? "C:\\Users\\Tim\\Documents\\Visual Studio 2015\\Projects\\MobileApiImages\\Wetlands\\" + fileName + ".zip" : "~/Resources/Images/Wetlands/" + fileName + ".zip";
                String filePath = HostingEnvironment.MapPath(directory);
                using (ZipFile zip = ZipFile.Read(filePath))
                {
                    var pushStreamContent = new PushStreamContent((stream, content, context) =>
                    {
                        zip.Save(stream);
                        stream.Close();
                    }, "application/zip");

                    HttpResponseMessage response = new HttpResponseMessage();
                    response.StatusCode = HttpStatusCode.OK;
                    response.Content = pushStreamContent;
                    response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName + ".zip" };

                    return response;
                }
            
           }

        //[AuthenticationTokenWetlands]
        [Route("api/wetland/images/{Id}")]
        public HttpResponseMessage GetImage(int Id)
        {

            ImagesWetland wetlandImage = wetlandDb.ImagesWetland.FirstOrDefault(b => b.id == Id);
            if (wetlandImage == null)
            {
                return this.Request.CreateResponse(HttpStatusCode.NotFound, "Image not found");
            }
            else
            {
                var filePath = "~/Resources/Images/Wetlands/" + wetlandImage.filename;
                var result = GetImageFile(filePath);
                return result;
            }
               
        }

        //[AuthenticationTokenWetlands]
        [Route("api/wetland/image_icons/{filename}")]
        public HttpResponseMessage GetImageIcon(string filename)
        {
            var filePath = "~/Resources/Images/Wetlands/" + filename + "_icon.jpg";
            var result = GetImageFile(filePath);
            return result;
        }

        //[AuthenticationTokenWetlands]
        [Route("api/wetland/range_images/{filename}")]
        public HttpResponseMessage GetRangeImage(string filename)
        {
            var filePath = "~/Resources/Images/Wetlands/" + filename + ".png";
            var result = GetImageFile(filePath);
            return result;
        }

        // GET api/Account/UploadCsvFile
        [HttpGet]
        [Route("api/uploadDataWetlands")]
        public async Task<IHttpActionResult> UploadDataWetlandsAsync()
        {

            string appRoot = HttpContext.Current.Server.MapPath("~");
            string filename = appRoot + "/DataFolder/wetlands_data_Copy.js";
            char[] result;
            StringBuilder builder = new StringBuilder();

            IList<WetlandPlant> wetlandPlants = new List<WetlandPlant>();

            using (StreamReader reader = File.OpenText(filename))
            {
                result = new char[reader.BaseStream.Length];
                await reader.ReadAsync(result, 0, (int)reader.BaseStream.Length);
            }

            foreach (char c in result)
            {

                    builder.Append(c);
                
            }


            JsonTextReader readers = new JsonTextReader(new StringReader(builder.ToString()));
            JObject obj = null;

            try
            {
                //Loading json data with json text reader
                while (readers.Read())
                {
                    if (readers.TokenType == JsonToken.StartObject)
                    {

                         obj = JObject.Load(readers);

                    }
                }
                
                
                if (obj.HasValues) {

                    var listOfAllPlants = (JArray)obj["plants"];

                    //Each one of these is new Wetland plant
                    foreach (JObject value in listOfAllPlants)
                    {
                        WetlandPlant newPlant = new WetlandPlant();

                        foreach (var property in value.Properties())
                        {
                            Console.WriteLine("  {0}: {1}", property.Name, property.Value);
                            setPlantAttribute(newPlant, property);                        
                        }

                        wetlandPlants.Add(newPlant);
                        wetlandDb.Plants.Add(newPlant);
                        wetlandDb.SaveChanges();
                    }


                  //  wetlandDb.Plants.AddRange(wetlandPlants);
                  //  wetlandDb.Plants.SaveChanges();
                }
            }

            catch (Exception e) {
                Console.WriteLine("Error {0}", e);
            }

            return null;
        }

        // GET api/uploadDataWetlandsGlossary
        [HttpGet]
        [Route("api/uploadDataWetlandsGlossary")]
        public async Task<IHttpActionResult> UploadDataWetlandsGlossaryAsync()
        {

            string appRoot = HttpContext.Current.Server.MapPath("~");
            string filename = appRoot + "/DataFolder/glossary.js";
            char[] result;
            StringBuilder builder = new StringBuilder();

            IList<WetlandGlossary> wetlandGlossaries = new List<WetlandGlossary>();

            using (StreamReader reader = File.OpenText(filename))
            {
                result = new char[reader.BaseStream.Length];
                await reader.ReadAsync(result, 0, (int)reader.BaseStream.Length);
            }

            foreach (char c in result)
            {

                builder.Append(c);

            }


            JsonTextReader readers = new JsonTextReader(new StringReader(builder.ToString()));
            JObject obj = null;

            try
            {
                //Loading json data with json text reader
                while (readers.Read())
                {
                    if (readers.TokenType == JsonToken.StartObject)
                    {

                        obj = JObject.Load(readers);

                    }
                }


                if (obj.HasValues)
                {

                    var listOfAllPlants = (JArray)obj["glossary"];

                    //Each one of these is new Wetland plant
                    foreach (JObject value in listOfAllPlants)
                    {
                        WetlandGlossary newGlossary = new WetlandGlossary();

                        foreach (var property in value.Properties())
                        {                            
                            setGlossaryAttribute(newGlossary, property);
                        }

                        wetlandGlossaries.Add(newGlossary);
                        wetlandDb.WetlandGlossary.Add(newGlossary);
                        wetlandDb.SaveChanges();
                    }


                    //  wetlandDb.Plants.AddRange(wetlandPlants);
                    //  wetlandDb.Plants.SaveChanges();
                }
            }

            catch (Exception e)
            {
                Console.WriteLine("Error {0}", e);
            }

            return null;
        }

        private HttpResponseMessage GetImageFile(string filePath)
        {
            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            String fileLocation = HostingEnvironment.MapPath(filePath);
            using (FileStream fileStream = new FileStream(fileLocation, FileMode.Open))
            {
                Image image = Image.FromStream(fileStream);
                MemoryStream memoryStream = new MemoryStream();
                image.Save(memoryStream, ImageFormat.Jpeg);
                result.Content = new ByteArrayContent(memoryStream.ToArray());
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            }

            return result;
        }

        private void setGlossaryAttribute(WetlandGlossary newGlossary, JProperty property)
        {
            switch (property.Name.ToLower())
            {
                case "id":
                    newGlossary.id = int.Parse(property.Value.ToString());
                    break;
                case "name":
                    newGlossary.name = property.Value.ToString();
                    break;
                case "definition":
                    newGlossary.definition = property.Value.ToString();
                    break;
            }

        }

        private void setPlantAttribute(WetlandPlant newPlant, JProperty property)
        {
            switch (property.Name.ToLower())
            {
                case "id":
                    newPlant.id = int.Parse(property.Value.ToString());
                    break;
                case "sections":
                    newPlant.sections = property.Value.ToString();
                    break;
                case "scinameauthor":
                    newPlant.scinameauthor = property.Value.ToString();
                    break;
                case "scinamenoauthor":
                    newPlant.scinamenoauthor = property.Value.ToString();
                    break;
                case "family":
                    newPlant.family = property.Value.ToString();
                    break;

                case "commonname":
                    newPlant.commonname = property.Value.ToString();
                    break;

                case "plantscode":
                    newPlant.plantscode = property.Value.ToString();
                    break;

                case "mapimg":
                    newPlant.mapimg = property.Value.ToString();
                    break;

                case "itiscode":
                    newPlant.itiscode = property.Value.ToString();
                    break;
                case "awwetcode":
                    newPlant.awwetcode = property.Value.ToString();
                    break;
                case "gpwetcode":
                    newPlant.gpwetcode = property.Value.ToString();
                    break;

                case "wmvcwetcode":
                    newPlant.wmvcwetcode = property.Value.ToString();
                    break;

                case "cvalue":
                    newPlant.cvalue = property.Value.ToString();
                    break;

                case "grank":
                    newPlant.grank = property.Value.ToString();
                    break;

                case "federalstatus":
                    newPlant.federalstatus = property.Value.ToString();
                    break;

                case "cosrank":
                    newPlant.cosrank = property.Value.ToString();
                    break;

                case "mtsrank":
                    newPlant.mtsrank = property.Value.ToString();
                    break;

                case "ndsrank":
                    newPlant.ndsrank = property.Value.ToString();
                    break;

                case "sdsrank":
                    newPlant.sdsrank = property.Value.ToString();
                    break;

                case "utsrank":
                    newPlant.utsrank = property.Value.ToString();
                    break;

                case "wysrank":
                    newPlant.wysrank = property.Value.ToString();
                    break;

                case "nativity":
                    newPlant.nativity = property.Value.ToString();
                    break;
                case "noxiousweed":
                    newPlant.noxiousweed = property.Value.ToString();
                    break;
                case "elevminfeet":
                    newPlant.elevminfeet = int.Parse(property.Value.ToString());
                    break;

                case "elevminm":
                    newPlant.elevminm = int.Parse(property.Value.ToString());
                    break;

                case "elevmaxfeet":
                    newPlant.elevmaxfeet = int.Parse(property.Value.ToString());
                    break;

                case "elevmaxm":
                    newPlant.elevmaxm = int.Parse(property.Value.ToString());
                    break;

                case "keychar1":
                    newPlant.keychar1 = property.Value.ToString();
                    break;

                case "keychar2":
                    newPlant.keychar2 = property.Value.ToString();
                    break;
                case "keychar3":
                    newPlant.keychar3 = property.Value.ToString();
                    break;
                case "keychar4":
                    newPlant.keychar4 = property.Value.ToString();
                    break;
                case "keychar5":
                    newPlant.keychar5 = property.Value.ToString();
                    break;

                case "keychar6":
                    newPlant.keychar6 = property.Value.ToString();
                    break;

                case "similarsp":
                    newPlant.similarsp = property.Value.ToString();
                    break;

                case "habitat":
                    newPlant.habitat = property.Value.ToString();
                    break;

                case "comments":
                    newPlant.comments = property.Value.ToString();
                    break;

                case "animaluse":
                    newPlant.animaluse = property.Value.ToString();
                    break;

                case "ecologicalsystems":
                    newPlant.ecologicalsystems = property.Value.ToString();
                    break;

                case "synonyms":
                    newPlant.synonyms = property.Value.ToString();
                    break;

                case "topimgtopimg":
                    newPlant.topimgtopimg = property.Value.ToString();
                    break;

                case "duration":
                    newPlant.duration = property.Value.ToString();
                    break;

                case "images":
                    populateWetlandImages(newPlant, property.Value);
                    break;

                case "similarspecies":
                    populateSimilarSpecies(newPlant, property.Value);
                    break;

                case "counties":
                    populateCounties(newPlant, property.Value);
                    break;
                case "references":
                    populateReferences(newPlant, property.Value);
                    break;

                case "fruits":
                    populateFruits(newPlant, property.Value);
                    break;
                case "division":
                    populateDivision(newPlant, property.Value);
                    break;
                case "shape":
                    populateShape(newPlant, property.Value);
                    break;
                case "arrangement":
                    populateArrangement(newPlant, property.Value);
                    break;
                case "size":
                    populateSize(newPlant, property.Value);
                    break;
                case "regions":
                    populateRegions(newPlant, property.Value);
                    break;
                default:
                    break;
            }
        }

        private void populateRegions(WetlandPlant newPlant, JToken value)
        {
            ICollection<RegionWetland> objList = new List<RegionWetland>();

            var listOfValues = (JArray)value;


            //Each one of these is new Wetland plant
            foreach (JObject valueItem in listOfValues)
            {
                RegionWetland newObj = new RegionWetland();

                foreach (var property in valueItem.Properties())
                    setRegionWetlandAttribute(newObj, property);

                    objList.Add(newObj);
            }


            newPlant.RegionWetland = objList;
        }

        private void setRegionWetlandAttribute(RegionWetland newObj, JProperty property)
        {
            switch (property.Name.ToLower())
            {
                case "valueid":
                    newObj.valueid = int.Parse(property.Value.ToString());
                    break;

                default:
                    break;

            }
        }

        private void populateSize(WetlandPlant newPlant, JToken value)
        {
            ICollection<SizeWetland> objList = new List<SizeWetland>();

            var listOfValues = (JArray)value;


            //Each one of these is new Wetland plant
            foreach (JObject valueItem in listOfValues)
            {
                SizeWetland newObj = new SizeWetland();

                foreach (var property in valueItem.Properties())
                    setSizeWetlandAtrribute(newObj, property);

                    objList.Add(newObj);

            }


            newPlant.SizeWetland = objList;
        }

        private void setSizeWetlandAtrribute(SizeWetland newObj, JProperty property)
        {
            switch (property.Name.ToLower())
            {
                case "valueid":
                    newObj.valueid = int.Parse(property.Value.ToString());
                    break;

                default:
                    break;

            }
        }

        private void populateArrangement(WetlandPlant newPlant, JToken value)
        {
            ICollection<ArrangementWetland> objList = new List<ArrangementWetland>();

            var listOfValues = (JArray)value;


            //Each one of these is new Wetland plant
            foreach (JObject valueItem in listOfValues)
            {
                ArrangementWetland newObj = new ArrangementWetland();

                foreach (var property in valueItem.Properties())
                    setArrangementWetlandAttribute(newObj, property);

                    objList.Add(newObj);
            }


            newPlant.ArrangementWetland = objList;
        }

        private void setArrangementWetlandAttribute(ArrangementWetland newObj, JProperty property)
        {
            switch (property.Name.ToLower())
            {
                case "valueid":
                    newObj.valueid = int.Parse(property.Value.ToString());
                    break;

                default:
                    break;

            }
        }

        private void populateShape(WetlandPlant newPlant, JToken value)
        {
            ICollection<ShapeWetland> objList = new List<ShapeWetland>();

            var listOfValues = (JArray)value;


            //Each one of these is new Wetland plant
            foreach (JObject valueItem in listOfValues)
            {
                ShapeWetland newObj = new ShapeWetland();

                foreach (var property in valueItem.Properties())
                    setShapeWetlandAttribute(newObj, property);

                    objList.Add(newObj);
            }


            newPlant.ShapeWetland = objList;
        }

        private void setShapeWetlandAttribute(ShapeWetland newObj, JProperty property)
        {
            switch (property.Name.ToLower())
            {
                case "valueid":
                    newObj.valueid = int.Parse(property.Value.ToString());
                    break;

                default:
                    break;

            }
        }

        private void populateDivision(WetlandPlant newPlant, JToken value)
        {
            ICollection<DivisionWetland> objList = new List<DivisionWetland>();

            var listOfValues = (JArray)value;


            //Each one of these is new Wetland plant
            foreach (JObject valueItem in listOfValues)
            {
                DivisionWetland newObj = new DivisionWetland();

                foreach (var property in valueItem.Properties())
                    setDivisionWetlandAttribute(newObj, property);

                    objList.Add(newObj);
            }


            newPlant.DivisionWetland = objList;
        }

        private void setDivisionWetlandAttribute(DivisionWetland newObj, JProperty property)
        {
            switch (property.Name.ToLower())
            {
                case "valueid":
                    newObj.valueid = int.Parse(property.Value.ToString());
                    break;

                default:
                    break;

            }
        }

        private void populateFruits(WetlandPlant newPlant, JToken value)
        {
            ICollection<FruitWetland> objList = new List<FruitWetland>();

            var listOfValues = (JArray)value;


            //Each one of these is new Wetland plant
            foreach (JObject valueItem in listOfValues)
            {
                FruitWetland newObj = new FruitWetland();

                foreach (var property in valueItem.Properties())
                 setFruitWetlandAttribute(newObj, property);

                    objList.Add(newObj);
             }


            newPlant.FruitWetland = objList;
        }

        private void setFruitWetlandAttribute(FruitWetland newObj, JProperty property)
        {
            switch (property.Name.ToLower())
            {
                case "valueid":
                    newObj.valueid = int.Parse(property.Value.ToString());
                    break;

                default:
                    break;

            }
        }

        private void populateReferences(WetlandPlant newPlant, JToken value)
        {
            ICollection<ReferenceWetland> objList = new List<ReferenceWetland>();

            var listOfValues = (JArray)value;

            foreach (JObject valueItem in listOfValues)
            {
                ReferenceWetland newObj = new ReferenceWetland();

                foreach (var property in valueItem.Properties())
                 setReferenceWetlandAttribute(newObj, property);

                    objList.Add(newObj);
            }

            newPlant.References = objList;
        }

        private void setReferenceWetlandAttribute(ReferenceWetland newObj, JProperty property)
        {
            switch (property.Name.ToLower())
            {
                case "id":
                    newObj.id = int.Parse(property.Value.ToString());
                    break;

                case "reference":
                    newObj.reference = property.Value.ToString();
                    break;

                case "fullcitation":
                    newObj.fullcitation = property.Value.ToString();
                    break;

                default:
                    break;

            }
        }

        private void populateCounties(WetlandPlant newPlant, JToken value)
        {
            ICollection<CountyPlantWetland> objList = new List<CountyPlantWetland>();

            var listOfValues = (JArray)value;

            //Each one of these is new Wetland plant
            foreach (JObject valueItem in listOfValues)
            {
                CountyPlantWetland newObj = new CountyPlantWetland();

                foreach (var property in valueItem.Properties())
                    setCountyPlantWetlandAttribute(newObj, property);

                    objList.Add(newObj);
            }


            newPlant.CountyPlantWetland = objList;
        }

        private void setCountyPlantWetlandAttribute(CountyPlantWetland newObj, JProperty property)
        {
            switch (property.Name.ToLower())
            {
                case "id":
                    newObj.county_id = int.Parse(property.Value.ToString());
                    break;

                case "name":
                    newObj.name = property.Value.ToString();
                    break;

                default:
                    break;

            }
        }

        private void populateSimilarSpecies(WetlandPlant newPlant, JToken value)
        {
            ICollection<SimilarSpeciesWetland> objList = new List<SimilarSpeciesWetland>();

            var listOfValues = (JArray)value;

            //Each one of these is new Wetland plant
            foreach (JObject valueItem in listOfValues)
            {
                SimilarSpeciesWetland newObj = new SimilarSpeciesWetland();

                foreach (var property in valueItem.Properties())                               
                    setSimilarSpeciesWetlandAttribute(newObj, property);
                
                objList.Add(newObj);
            }

            newPlant.SimilarSpecies = objList;
        }

        private void setSimilarSpeciesWetlandAttribute(SimilarSpeciesWetland newObj, JProperty property)
        {
            switch (property.Name.ToLower())
            {
                case "id":
                    newObj.id = int.Parse(property.Value.ToString());
                    break;

                case "similarspicon":
                    newObj.similarspicon = property.Value.ToString();
                    break;

                case "similarspscinameauthor":
                    newObj.similarspscinameauthor = property.Value.ToString();
                    break;

                case "reason":
                    newObj.reason = property.Value.ToString();
                    break;

                default:
                    break;

            }
        }


        private void populateWetlandImages(WetlandPlant newPlant, JToken value)
        {
            ICollection<ImagesWetland> images = new List<ImagesWetland>();

            var listOfImages = (JArray)value;


            //Each one of these is new Wetland plant
            foreach (JObject imageValue in listOfImages)
            {
                ImagesWetland newImage = new ImagesWetland();

                foreach (var property in imageValue.Properties())
                {
     
                    setWetlantImageAttribute(newImage, property);                
                }

                images.Add(newImage);

            }


            newPlant.Images = images;
        }

        private void setWetlantImageAttribute(ImagesWetland newImage, JProperty property)
        {
            switch (property.Name.ToLower())
            {
                case "id":
                    newImage.id = int.Parse(property.Value.ToString());
                    break;

                case "filename":
                    newImage.filename = property.Value.ToString();
                    break;

                case "credit":
                    newImage.credit = property.Value.ToString();
                    break;

                default:
                    break;

            }

         }

        // GET api/uploadData
        [HttpGet]
        [Route("api/uploadData")]
        public IHttpActionResult UploadData()
        {
            string appRoot = HttpContext.Current.Server.MapPath("~");
            string sSheetName = "Sheet1";
            string sConnection = null;
            DataTable dtTablesList = default(DataTable);
            OleDbCommand oleExcelCommand = default(OleDbCommand);
            OleDbDataReader oleExcelReader = default(OleDbDataReader);
            OleDbConnection oleExcelConnection = default(OleDbConnection);
            IList<KeyValuePair<String, Int32>> idNamePair = new List<KeyValuePair<String, Int32>>();//Need to have id as number because names are way too long
            //Int32 currentId = 0;
            Int32 uniqueIdNum = 1;

            sConnection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + appRoot + "/DataFolder/woody_plant_db.xlsx" + ";Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\"";

            oleExcelConnection = new OleDbConnection(sConnection);
            oleExcelConnection.Open();

            dtTablesList = oleExcelConnection.GetSchema("Tables");

            if (dtTablesList.Rows.Count > 0)
            {
                sSheetName = dtTablesList.Rows[0]["TABLE_NAME"].ToString();
            }

            dtTablesList.Clear();
            dtTablesList.Dispose();


            if (!string.IsNullOrEmpty(sSheetName))
            {

                oleExcelCommand = oleExcelConnection.CreateCommand();
                oleExcelCommand.CommandText = "Select * From [" + sSheetName + "]";
                oleExcelCommand.CommandType = CommandType.Text;
                oleExcelReader = oleExcelCommand.ExecuteReader();

                var firstRecord = true;
                while (oleExcelReader.Read())
                {
                    if (!firstRecord)
                    {
                        WoodyPlant newPlant = new WoodyPlant
                        {
                            plant_imported_id = Convert.ToInt32(oleExcelReader.GetValue(0)),
                            family = oleExcelReader.GetValue(1).ToString(),
                            scientificNameWeber = oleExcelReader.GetValue(2).ToString(),
                            leafType = oleExcelReader.GetValue(3).ToString(),
                            leafShape = oleExcelReader.GetValue(4).ToString(),
                            growthForm = oleExcelReader.GetValue(5).ToString(),
                            growthDuration = oleExcelReader.GetValue(6).ToString(),
                            scientificNameOther = oleExcelReader.GetValue(7).ToString(),
                            commonName = oleExcelReader.GetValue(8).ToString(),
                            commonNameSecondary = oleExcelReader.GetValue(9).ToString(),
                            scientificNameMeaningWeber = oleExcelReader.GetValue(10).ToString(),
                            plantClass = oleExcelReader.GetValue(11).ToString(),
                            plantSubClass = oleExcelReader.GetValue(12).ToString(),
                            origin = oleExcelReader.GetValue(13).ToString(),
                            weedManagement = oleExcelReader.GetValue(14).ToString(),
                            edibility = oleExcelReader.GetValue(15).ToString(),
                            livestock = oleExcelReader.GetValue(16).ToString(),
                            toxicity = oleExcelReader.GetValue(17).ToString(),
                            ecologicalRelationships = oleExcelReader.GetValue(18).ToString(),
                            frequency = oleExcelReader.GetValue(19).ToString(),
                            habitat = oleExcelReader.GetValue(20).ToString(),
                            scientificNameNelson = oleExcelReader.GetValue(21).ToString(),
                            scientificNameMeaningNelson = oleExcelReader.GetValue(22).ToString(),
                            seasonOfBloom = oleExcelReader.GetValue(23).ToString(),
                            familyCharacteristics = oleExcelReader.GetValue(24).ToString(),
                            flowerSymmetry = oleExcelReader.GetValue(25).ToString(),
                            flowerCluster = oleExcelReader.GetValue(26).ToString(),
                            flowerShape = oleExcelReader.GetValue(27).ToString(),
                            commonNameDerivation = oleExcelReader.GetValue(28).ToString(),
                            landscapingCultivar = oleExcelReader.GetValue(29).ToString(),
                            flowerColor = oleExcelReader.GetValue(30).ToString(),
                            fruitColor = oleExcelReader.GetValue(31).ToString(),
                            availability = oleExcelReader.GetValue(32).ToString(),
                            keyCharacteristics = oleExcelReader.GetValue(33).ToString(),
                            lifeZone = oleExcelReader.GetValue(34).ToString(),
                            endemicLocation = oleExcelReader.GetValue(35).ToString(),
                            landscapingUse = oleExcelReader.GetValue(36).ToString(),
                            matureHeight = oleExcelReader.GetValue(37).ToString(),
                            matureSpread = oleExcelReader.GetValue(38).ToString(),
                            lightRequirements = oleExcelReader.GetValue(39).ToString(),
                            soilRequirements = oleExcelReader.GetValue(40).ToString(),
                            fiber = oleExcelReader.GetValue(41).ToString(),
                            otherInformation = oleExcelReader.GetValue(42).ToString(),
                            flowerSize = oleExcelReader.GetValue(43).ToString(),
                            petalNumber = oleExcelReader.GetValue(44).ToString(),
                            flowerStructure = oleExcelReader.GetValue(45).ToString(),
                            moistureRequirements = oleExcelReader.GetValue(46).ToString(),
                            pronunciation = oleExcelReader.GetValue(47).ToString(),
                            fruitType = oleExcelReader.GetValue(48).ToString(),
                            subspecies = oleExcelReader.GetValue(49).ToString(),
                            variety = oleExcelReader.GetValue(50).ToString(),
                            forma = oleExcelReader.GetValue(51).ToString(),
                            legalStatus = oleExcelReader.GetValue(52).ToString(),
                            guanellaPass = oleExcelReader.GetValue(53).ToString(),
                            plainCc = oleExcelReader.GetValue(54).ToString(),
                            noNameCreek = oleExcelReader.GetValue(55).ToString(),
                            maloitPark = oleExcelReader.GetValue(56).ToString(),
                            vailNc = oleExcelReader.GetValue(57).ToString(),
                            lovelandPass = oleExcelReader.GetValue(58).ToString(),
                            roxborough = oleExcelReader.GetValue(59).ToString(),
                            castlewood = oleExcelReader.GetValue(60).ToString(),
                            custerCounty = oleExcelReader.GetValue(61).ToString(),
                            dbg = oleExcelReader.GetValue(62).ToString(),
                            grassesAtGreenMtn = oleExcelReader.GetValue(63).ToString(),
                            eastPortal = oleExcelReader.GetValue(64).ToString(),
                            mesaCounty = oleExcelReader.GetValue(65).ToString(),
                            tellerCounty = oleExcelReader.GetValue(66).ToString(),
                            goldenGate = oleExcelReader.GetValue(67).ToString(),
                            southPlattePark = oleExcelReader.GetValue(68).ToString(),
                            greenMt = oleExcelReader.GetValue(69).ToString(),
                            reynolds = oleExcelReader.GetValue(70).ToString(),
                            grassesManual = oleExcelReader.GetValue(71).ToString(),
                            falcon = oleExcelReader.GetValue(72).ToString(),
                            lookoutMt = oleExcelReader.GetValue(73).ToString(),
                            southValley = oleExcelReader.GetValue(74).ToString(),
                            deerCreek = oleExcelReader.GetValue(75).ToString(),
                            lairOTheBear = oleExcelReader.GetValue(76).ToString(),
                            print = oleExcelReader.GetValue(77).ToString(),
                            highPlains = oleExcelReader.GetValue(78).ToString(),
                            shrubs = oleExcelReader.GetValue(79).ToString()
                        };
                        woodyDb.Plants.Add(newPlant);
                        woodyDb.SaveChanges();
                        Debug.WriteLine("{0}: {1}", newPlant.plant_id, newPlant.scientificNameWeber);

                        uniqueIdNum++;
                    }

                    firstRecord = false;
                }

                oleExcelReader.Close();
            }
            oleExcelConnection.Close();

            JavaScriptSerializer systemSerializer = new JavaScriptSerializer();
            systemSerializer.MaxJsonLength = Int32.MaxValue;

            return null;
        }



    }
}