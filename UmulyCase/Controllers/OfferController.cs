using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using UmulyCase.Models;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore.SqlServer.Scaffolding.Internal;
using System.Linq;
using System;
using System.Dynamic;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using Microsoft.Data.SqlClient.Server;
//using Newtonsoft.Json;

namespace UmulyCase.Controllers
{
    public class OfferController : Controller
    {
        private string ConnectString = String.Empty;
        private readonly ILogger<OfferController> _logger;
        public OfferController(IConfiguration configuration, ILogger<OfferController> logger)
        {
            // Get connection string
            ConnectString = configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
            _logger = logger;
        }

        // GET: OfferController
        public ActionResult Index()
        {
            List<OfferViewModel> offer = new List<OfferViewModel>();
            try
            {
                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    string sql = "sp_GetOffers";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                 var value = JsonSerializer.Deserialize<List<OfferViewModel>>((string)reader.GetValue(0));
                                 if(value != null)
                                {
                                    offer = value;
                                }
                              
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {

                ViewBag.ErrorMessage = ex.Message;
            }


            return View(offer);
        }

        // GET: OfferController/Details/5
        public ActionResult Details(int id)
        {
            OfferViewModel offer =new OfferViewModel();
            try
            {
                if (id <= 0)
                    throw new Exception("Invalid parameter");
                 
                offer = GetOffer(id);


            }
            catch (Exception ex)
            {

                ViewBag.ErrorMessage = ex.Message;
            }

            return View(offer);
        }

        // GET: OfferController/Create
        public ActionResult Create()
        {
            dynamic EditOfferModel = GetDynamicObject(0);
            return View(EditOfferModel);
        }

        //POST
        [HttpPost]
        public ActionResult Create(OfferViewModel offer)
        {
            try
            {
                if (offer == null)
                {
                    return RedirectToAction("Create", "Offer");
                }
                int Id;
                bool hasError = false;

                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    //Offer bilgilerinin veritabanına kaydı
                    string sqlOffer = "sp_AddOffer";
                    
                    var prmDate = new SqlParameter()
                    {
                        ParameterName = "@date",
                        Value = offer.OfferDate,
                        DbType = DbType.Date
                    };
                    var prmDescription = new SqlParameter()
                    {
                        ParameterName = "@description",
                        Value = offer.Description,
                        DbType = DbType.String,
                    };
                    var prmUserName = new SqlParameter()
                    {
                        ParameterName = "@userName",
                        Value = offer.UserName,
                        DbType = DbType.String,
                    };
                  
                    using (SqlCommand cmd = new SqlCommand(sqlOffer, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        
                        cmd.Parameters.Add(prmDescription);
                        cmd.Parameters.Add(prmUserName);
                        cmd.Parameters.Add(prmDate);
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                Id = (int)reader.GetValue(0);
                                hasError = Id == -1;
                            }
                        }
                        reader.Close();
                        if (hasError)
                        {
                            throw new Exception("Offer not added.");
                        }
                    }
                    //Offer Details bilgilerinin veritabanına kaydı
                    string sqlOfferDetails = "sp_AddOrUpdateOrDeleteOfferDetails";
                    using (SqlCommand cmd = new SqlCommand(sqlOfferDetails, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        SqlParameter prmDetail =new SqlParameter() { 
                            SqlDbType = SqlDbType.Structured, 
                            Value =GetOfferDetails( offer.Details),
                            ParameterName= "@details",
                            TypeName= "OfferDetailTable",
                            Direction = ParameterDirection.Input
                        };
                        cmd.Parameters.Add(prmDetail);
                        
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                int value = (int)reader.GetValue(0);
                                hasError = value != 0;
                            }
                        }
                        reader.Close();
                        if (hasError)
                        {
                            throw new Exception("Offer Details not added.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                ViewBag.ErrorMessage = ex.Message;
            }

            return RedirectToAction("Index", "Offer");
        }

        // GET: OfferController/Edit/5
        [HttpGet]
        public ActionResult Edit(int id)
        {
           
            dynamic EditOfferModel = GetDynamicObject(id);
            
            //try
            //{
            //    if (id <= 0)
            //    {
            //        return BadRequest();
            //    }
                
            //    EditOfferModel.Offer = GetOffer(id);
            //    EditOfferModel.Modes = GetModes();
            //    EditOfferModel.Incoterms = GetIncoterms();
            //    EditOfferModel.Units = GetUnits();
            //    EditOfferModel.Movements = GetMovements();
            //    EditOfferModel.Packages = GetPackages();
            //    EditOfferModel.Currencies = GetCurrencies();
            //    var opt = new JsonSerializerOptions() { WriteIndented = true };
            //    string strJson = JsonSerializer.Serialize<List<CountryViewModel>>(GetCountries(), opt);
            //    EditOfferModel.Countries = strJson;
            //    string strJson2 = JsonSerializer.Serialize<List<OfferViewModel>>(GetOfferList(id), opt);
            //    EditOfferModel.OfferJson = strJson2;


            //}
            //catch (Exception ex)
            //{

            //    ViewBag.ErrorMessage = ex.Message;
            //}
            return View(EditOfferModel);
        }
       
        [HttpPost]
        public ActionResult Update(OfferViewModel offer)
        {
            try
            {
                if (offer == null)
                {
                    return RedirectToAction("Edit", "Offer");
                }
                int Id;
                bool hasError = false;

                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    //Offer bilgilerinin veritabanına kaydı
                    string sqlOffer = "sp_UpdateOffer";
                    var prmId = new SqlParameter()
                    {
                        ParameterName = "@Id",
                        Value = offer.Id,
                        DbType = DbType.Int32,
                    };
                    var prmDate = new SqlParameter()
                    {
                        ParameterName = "@date",
                        Value = offer.OfferDate,
                        DbType=DbType.Date
                    };
                    var prmDescription = new SqlParameter()
                    {
                        ParameterName = "@description",
                        Value = offer.Description,
                        DbType = DbType.String,
                    };
                    var prmUserName = new SqlParameter()
                    {
                        ParameterName = "@userName",
                        Value = offer.UserName,
                        DbType=DbType.String,
                    };

                    using (SqlCommand cmd = new SqlCommand(sqlOffer, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(prmId);
                        cmd.Parameters.Add(prmDescription);
                        cmd.Parameters.Add(prmUserName);
                        cmd.Parameters.Add(prmDate);
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                Id = (int)reader.GetValue(0);
                                hasError = Id != 0;
                            }
                        }
                        reader.Close();
                        if (hasError)
                        {
                            throw new Exception("Offer not updated.");
                        }
                    }
                    //Offer Details bilgilerinin veritabanına kaydı
                    string sqlOfferDetails = "sp_AddOrUpdateOrDeleteOfferDetails";
                    using (SqlCommand cmd = new SqlCommand(sqlOfferDetails, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        SqlParameter prmDetail = new SqlParameter()
                        {
                            SqlDbType = SqlDbType.Structured,
                            Value = GetOfferDetails(offer.Details),
                            ParameterName = "@details",
                            TypeName = "OfferDetailTable",
                            Direction=ParameterDirection.Input
                        };
                        cmd.Parameters.Add(prmDetail);

                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                int value = (int)reader.GetValue(0);
                                hasError = value != 0;
                            }
                        }
                        reader.Close();
                        if (hasError)
                        {
                            throw new Exception("Offer Details not added.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                ViewBag.ErrorMessage = ex.Message;
            }

            return RedirectToAction("Index", "Offer");
        }


        // GET: OfferController/Delete/5
        public ActionResult Delete(int id)
        {
            try
            {
                if (id <=0)
                {
                    return RedirectToAction("Delete", "Offer");
                }
                int retunValue = 0;
                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    string sql = "sp_DeleteOffer";
                    var prmId = new SqlParameter()
                    {
                        ParameterName = "@Id",
                        Value = id,
                    };

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(prmId);

                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                retunValue = (int)reader.GetValue(0);
                            }
                        }

                    }
                }

                if (retunValue == -1)
                {
                    throw new Exception("Offer not deleted.");
                }
            }
            catch (Exception ex)
            {

                ViewBag.ErrorMessage = ex.Message;
            }
            return RedirectToAction("Index", "Offer");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private dynamic GetDynamicObject(int id)
        {
            dynamic EditOfferModel = new ExpandoObject();
            try
            {
                if (id==0)
                {
                    EditOfferModel.Offer = new OfferViewModel();
                }
                else
                {
                    EditOfferModel.Offer = GetOffer(id);
                }
                
                EditOfferModel.Modes = GetModes();
                EditOfferModel.Incoterms = GetIncoterms();
                EditOfferModel.Units = GetUnits();
                EditOfferModel.Movements = GetMovements();
                EditOfferModel.Packages = GetPackages();
                EditOfferModel.Currencies = GetCurrencies();
                var opt = new JsonSerializerOptions() { WriteIndented = true };
                string strJson = JsonSerializer.Serialize<List<CountryViewModel>>(GetCountries(), opt);
                EditOfferModel.Countries = strJson;
                string strJson2 = JsonSerializer.Serialize<List<OfferViewModel>>(id==0? new List<OfferViewModel>():GetOfferList(id), opt);
                EditOfferModel.OfferJson = strJson2;


            }
            catch (Exception ex)
            {

              
            }
            return EditOfferModel;
        }
        private OfferViewModel GetOffer(int id)
        {
            OfferViewModel offer = new OfferViewModel();
            try
            {
                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    string sql = "sp_GetOfferDetail";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter()
                        {
                            ParameterName = "@Id",
                            Value = id
                        });
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                var test = reader.GetString(0);
                                var result = JsonSerializer.Deserialize<List<JsonObjectData>>(reader.GetString(0));
                                if (result != null)
                                {
                                    foreach (var item in result)
                                    {
                                        offer.Id = item.Id;
                                        offer.OfferDate = Convert.ToDateTime(item.OfferDate);
                                        offer.Description = item.Description;
                                        offer.UserName = item.UserName;
                                        if (item.Details != null)
                                        {
                                            int i = 0;
                                            offer.Details = new List<OfferDetailViewModel>();
                                            foreach (var detail in item.Details)
                                            {
                                                offer.Details.Insert(i, new OfferDetailViewModel()
                                                {
                                                    Id = detail.Id,
                                                    OfferId = detail.OfferId,
                                                    Mode = detail.Mode[0],
                                                    Incoterm = detail.Incoterm[0],
                                                    Movement = detail.Movement[0],
                                                    PackageType = detail.PackageType[0],
                                                    Unit = detail.Unit[0],
                                                    Currency = detail.Currency[0],
                                                    Country = detail.Country[0],
                                                    City = detail.City[0]

                                                });
                                                i += 1;
                                            }
                                            // offer.Details = item.Details;
                                        }

                                    }
                                }

                            }
                        }

                    }
                }
            }
            catch (Exception)
            {

               //
            }

            return offer;
        }

        private DataTable GetOfferDetails(List<OfferDetailViewModel>? details)
        {
            DataTable sqlData = new DataTable();
            DataColumn Id = new DataColumn("Id");
            Id.DataType = Type.GetType("System.Int32");
            sqlData.Columns.Add(Id);
             DataColumn OfferId = new DataColumn("OfferId");
            OfferId.DataType = Type.GetType("System.Int16");
            sqlData.Columns.Add(OfferId);
            DataColumn ModeId = new DataColumn("ModeId");
            ModeId.DataType = Type.GetType("System.Int16");
            sqlData.Columns.Add(ModeId);
            DataColumn Movement =new DataColumn("MovementTypeId");
            Movement.DataType = Type.GetType("System.Int16");
            sqlData.Columns.Add(Movement);
            DataColumn Incoterm = new DataColumn("IncotermId");
            Incoterm.DataType = Type.GetType("System.Int16");
            sqlData.Columns.Add(Incoterm);
            DataColumn Package = new DataColumn("PackageTypeId");
            Package.DataType = Type.GetType("System.Int16");
            sqlData.Columns.Add(Package);
            DataColumn Unit = new DataColumn("UnitId");
            Unit.DataType = Type.GetType("System.Int16");
            sqlData.Columns.Add(Unit);
            DataColumn Currency = new DataColumn("CurrencyId");
            Currency.DataType = Type.GetType("System.Int16");
            sqlData.Columns.Add(Currency);
            DataColumn Country = new DataColumn("CountryId");
            Country.DataType = Type.GetType("System.Int16");
            sqlData.Columns.Add(Country);
            DataColumn City = new DataColumn("CityId");
            City.DataType = Type.GetType("System.Int16");
            sqlData.Columns.Add(City);

            if (details != null)
            {
                foreach (var item in details)
                {
                    DataRow dr = sqlData.NewRow();
                    dr["Id"] = item.Id;
                    dr["OfferId"] = item.OfferId;
                    dr["ModeId"] = item.Mode.ModeId;
                    dr["MovementTypeId"] = item.Movement.MovementId;
                    dr["IncotermId"] = item.Incoterm.IncotermId;
                    dr["PackageTypeId"] = item.PackageType.PackageId;
                    dr["UnitId"] = item.Unit.UnitId;
                    dr["CurrencyId"] = item.Currency.CurrencyId;
                    dr["CountryId"] = item.Country.CountryId;
                    dr["CityId"] = item.City.CityId;
                    sqlData.Rows.Add(dr);
                }
            }
           
            return sqlData;
        }
        private List<OfferViewModel> GetOfferList(int id)
        {
            List<OfferViewModel> offer =new List<OfferViewModel>();
            try
            {
                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    string sql = "sp_GetOfferDetail";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter()
                        {
                            ParameterName = "@Id",
                            Value = id
                        });
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                var test = reader.GetString(0);
                                var result = JsonSerializer.Deserialize<List<JsonObjectData>>(reader.GetString(0));
                                if (result != null)
                                {
                                    foreach (var item in result)
                                    {
                                        int a = 0;
                                        offer.Insert(a,new OfferViewModel()
                                        {
                                            Id = item.Id,
                                            OfferDate = Convert.ToDateTime(item.OfferDate),
                                            Description = item.Description,
                                            UserName = item.UserName
                                        });
                                    
                                        if (item.Details != null)
                                        {
                                            int i = 0;
                                            offer[a].Details = new List<OfferDetailViewModel>();
                                            foreach (var detail in item.Details)
                                            {
                                                offer[a].Details.Insert(i, new OfferDetailViewModel()
                                                {
                                                    Id = detail.Id,
                                                    OfferId = detail.OfferId,
                                                    Mode = detail.Mode[0],
                                                    Incoterm = detail.Incoterm[0],
                                                    Movement = detail.Movement[0],
                                                    PackageType = detail.PackageType[0],
                                                    Unit = detail.Unit[0],
                                                    Currency = detail.Currency[0],
                                                    Country = detail.Country[0],
                                                    City = detail.City[0]

                                                });
                                                i += 1;
                                            }
                                            // offer.Details = item.Details;
                                        }

                                    }
                                }

                            }
                        }

                    }
                }
            }
            catch (Exception)
            {
                //
            }

            return offer;
        }
        private List<ModeViewModel> GetModes()
        {
            List<ModeViewModel> modes = new List<ModeViewModel>();
            try
            {
                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    string sql = "sp_GetModes";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                var result = JsonSerializer.Deserialize<List<ModeViewModel>>(reader.GetString(0));
                                if (result != null)
                                {
                                    modes = result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return modes;
        }
        private List<IncotermViewModel> GetIncoterms()
        {
            List<IncotermViewModel> incoterms = new List<IncotermViewModel>();
            try
            {
                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    string sql = "sp_GetIncoterms";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                var result = JsonSerializer.Deserialize <List<IncotermViewModel>>(reader.GetString(0));
                                if (result != null)
                                {
                                    incoterms = result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

               //
            }

            return incoterms;
        }

        private List<MovementTypeViewModel> GetMovements()
        {
            List<MovementTypeViewModel> movements = new List<MovementTypeViewModel>();
            try
            {
                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    string sql = "sp_GetMovements";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                var result = JsonSerializer.Deserialize <List<MovementTypeViewModel>>(reader.GetString(0));
                                if (result != null)
                                {
                                    movements = result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

            return movements;
        }

        private List<PackageTypeViewModel> GetPackages()
        {
            List<PackageTypeViewModel> packages = new List<PackageTypeViewModel>();
            try
            {
                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    string sql = "sp_GetPackageTypes";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                var result = JsonSerializer.Deserialize<List<PackageTypeViewModel>>(reader.GetString(0));
                                if (result != null)
                                {
                                    packages = result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

                //
            }
            return packages;
        }

        private List<UnitViewModel> GetUnits()
        {
            List<UnitViewModel> units = new List<UnitViewModel>();
            try
            {
                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    string sql = "sp_GetUnits";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                var result = JsonSerializer.Deserialize <List<UnitViewModel>>(reader.GetString(0));
                                if (result != null)
                                {
                                    units = result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                    //
            }

            return units;
        }

        private List<CurrencyViewModel> GetCurrencies()
        {
            List<CurrencyViewModel> currencies = new List<CurrencyViewModel>();

            try
            {
                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    string sql = "sp_GetCurrencies";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                var result = JsonSerializer.Deserialize <List<CurrencyViewModel>>(reader.GetString(0));
                                if (result != null)
                                {
                                    currencies = result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                //
            }
            return currencies;
        }

        private List<CountryViewModel> GetCountries()
        {
            List<CountryViewModel> countries = new List<CountryViewModel>();

            try
            {
                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    string sql = "sp_GetCountries";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                var result = JsonSerializer.Deserialize <List<CountryViewModel>>(reader.GetString(0));
                                if (result != null)
                                {
                                    countries = result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

                //
            }
            return countries;
        }

        private List<CityViewModel> GetCities(int CountryId)
        {
            List<CityViewModel> cities = new List<CityViewModel>();
            try
            {
                using (SqlConnection con = new SqlConnection(this.ConnectString))
                {
                    con.Open();
                    string sql = "sp_GetCities";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            if (reader.HasRows)
                            {
                                var result = JsonSerializer.Deserialize<List<CityViewModel>>(reader.GetString(0));
                                if (result != null)
                                {
                                    cities = result;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

            return cities;
        }
    }
}
