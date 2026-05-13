using jitu_dashboard.Server.Message.Response;
using jitu_dashboard.Server.Services.PaymentHistory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace jitu_dashboard.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PaymentHistoryController : ControllerBase
    {
        private readonly ILogger<PaymentHistoryController> _logger;
        private readonly IPaymentHistoryService _serviceTransferOutright;

        public PaymentHistoryController(ILogger<PaymentHistoryController> logger, 
                                        IPaymentHistoryService serviceTransferOutright)
        {
            _logger = logger;
            _serviceTransferOutright = serviceTransferOutright;
        }

        [HttpGet(Name = "retrievePaymentHistory")]
        // public async Task<IActionResult> Retrieve(RequestModel requestModel)
        public async Task<IActionResult> Retrieve()
        {
            //_logger.LogInformation("Call S4TransferOutright API :  Get transfer to STPG by Transaction Status.");
            ResponseModel model = new ResponseModel();
            try
            {
                model.result = await _serviceTransferOutright.Retrieve();
                if (model.result != null)
                    model.code = "00";
                else
                    model.code = "01";
                model.success = true;
                model.message = "Get data successfully";
                return new JsonResult(model);
            }
            catch (ApplicationException ex)
            {
                model.code = "01";
                model.success = false;
                model.message = ex.Message;
                return new JsonResult(model);
            }
            catch (Exception ex)
            {
                model.code = "01";
                model.success = false;
                model.message = "Get data failed";
                return new JsonResult(model);
            }
        }
    }
}
