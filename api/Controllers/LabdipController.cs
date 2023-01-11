using BrandixAutomation.Labdip.API.ProcessFiles;
using BrandixAutomation.Labdip.API.Models;
using BrandixAutomation.Labdip.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;

namespace BrandixAutomation.Labdip.API.Controllers
{
    //dotnet run
    [Route("api/[controller]")]
    [ApiController]
    public class LabdipController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            //Test Api is Up and Running
            // http://localhost:5000/api/Labdip
            return StatusCode(200, "Labdip Chart Api Connected!");
        }

        [HttpPost,DisableRequestSizeLimit]
        public IActionResult Upload()
        {
            try
            {
                var file = Request.Form.Files[0];
                if(file.Length > 0)
                {
                    //Call the labdip process
                    LabdipChartDataService service = new LabdipChartDataService();

                    return Ok(service.GetLabdipChartData(file) );
                }
                else
                {
                    return BadRequest();
                }                
            }
            catch (System.Exception ex)
            {

                return StatusCode(500, $"Internal Server error:{ex.Message}");
            }
        }

        [HttpPost("labdipChart"), DisableRequestSizeLimit]
        public AutomationResponse<IEnumerable<LabdipChartModel>> LabdipChartProcess()
        {
            var response = new AutomationResponse<IEnumerable<LabdipChartModel>>();
            try
            {
                if ((ModelState.IsValid) && (Request.Form.Files.Count == 1) && (Request.Form.Files[0] != null))
                {
                        var file = Request.Form.Files[0];
                        if (file.Length > 0)
                        {
                            //Call the labdip process
                            LabdipChartDataService service = new LabdipChartDataService();
                            response.Data = service.GetLabdipChartData(file);
                        }
                }
                else
                {
                    throw new Exception("Bad Request Or File is null");
                }
            }
            
            catch (System.Exception ex)
            {
                response.SetResponseStatus(false, ex);
            }
            return response;
        }

        [HttpPost("threadShade"), DisableRequestSizeLimit]
        public AutomationResponse<ThreadShadeResponse> ThreadShadeProcess()
        {
            var response = new AutomationResponse<ThreadShadeResponse>();
            try
            {
                if (Request.Form.Files.Count > 1)
                {
                    var labdipChart = Request.Form.Files[0];
                    var threadShade = Request.Form.Files[1];
                    string threadTypes = Convert.ToString(Request.Form["ThreadTypes"]);

                    ThreadShadeDataService threadShadeDataService = new ThreadShadeDataService(labdipChart, threadShade, threadTypes);
                    response.Data = threadShadeDataService.ProcessThreadShadeData();
                }
                else
                    throw new Exception("Bad Request or File Error");
            }
            catch (Exception ex)
            {
                response.SetResponseStatus(false, ex);
            }
            return response;
        }

        [HttpGet("threadTypes")]
        public AutomationResponse<List<ThreadTypes>> GetThreadTypes()
        {
            var response = new AutomationResponse<List<ThreadTypes>>();
            try
            {
                ThreadTypeService threadTypeService = new ThreadTypeService();
                response.Data = threadTypeService.GetThreadTypes();
            }
            catch (Exception ex)
            {

                response.SetResponseStatus(false,ex);
            }
            return response;
        }

        [HttpPost("insertNewThread")]
        public AutomationResponse<List<ThreadTypes>> InsertNewThread([FromBody]AutomationRequest<ThreadTypes> request)
        {
            var response = new AutomationResponse<List<ThreadTypes>>();
            try
            {
                ThreadTypeService threadTypeService = new ThreadTypeService();
                if (threadTypeService.InsertNewRecord(request.Request))
                {
                    response.Data = threadTypeService.GetThreadTypes();
                }
            }
            catch (Exception ex)
            {
                response.SetResponseStatus(false, ex);
            }
            return response;
        }

        [HttpPost("updateThread")]
        public AutomationResponse<List<ThreadTypes>> UpdateThread([FromBody]AutomationRequest<ThreadTypes> request)
        {
            var response = new AutomationResponse<List<ThreadTypes>>();
            try
            {
                ThreadTypeService threadTypeService = new ThreadTypeService();
                if (threadTypeService.UpdateRecord(request.Request))
                {
                    response.Data = threadTypeService.GetThreadTypes();
                }
            }
            catch (Exception ex)
            {
                response.SetResponseStatus(false, ex);
            }
            return response;
        }

        [HttpPost("deleteThread")]
        public AutomationResponse<List<ThreadTypes>> deleteThread([FromBody]AutomationRequest<ThreadTypes> request)
        {
            var response = new AutomationResponse<List<ThreadTypes>>();
            try
            {
                ThreadTypeService threadTypeService = new ThreadTypeService();
                if (threadTypeService.DeleteRecord(request.Request))
                {
                    response.Data = threadTypeService.GetThreadTypes();
                }
            }
            catch (Exception ex)
            {
                response.SetResponseStatus(false, ex);
            }
            return response;
        }

    }
}
