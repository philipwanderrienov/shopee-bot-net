using jitu_dashboard.Server.Models;
using Microsoft.AspNetCore.Mvc;
using jitu_dashboard.Server.Message.Request;
using jitu_dashboard.Server.Message.Response;
using jitu_dashboard.Server.Services.Payment;

namespace jitu_dashboard.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentHistoryController> _logger;
        private readonly IPaymentService _serviceTransferOutright;

        public PaymentController(IPaymentService serviceTransferOutright)
        {
            _serviceTransferOutright = serviceTransferOutright;
        }

        [HttpGet(Name = "retrievePayment")]
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