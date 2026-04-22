using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        public MessageController()
        {
        }
    }
}
