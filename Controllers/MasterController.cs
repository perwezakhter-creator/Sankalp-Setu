using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SankalpAPI.Interface;
using SankalpAPI.Model;
using SankalpAPI.Repository;
using SankalpAPI.Security;
using SankalpAPI.Services;
using SankalpAPI.DTO;

namespace SankalpAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class MasterController : ControllerBase
    {
        private readonly MasterService _masterService;
        public MasterController(MasterService masterService)
        {
            _masterService = masterService;
        }

        // ============================================================
        // GET Gender List
        // ============================================================
        [HttpPost("GetGenderList")]
        public IActionResult GetGenderList([FromBody] MasterDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ClientKey))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request."
                    });
                }

                // ----------------------------------------------------
                // RSA DECRYPT CLIENT KEY
                // ----------------------------------------------------
                string key = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid client key."
                    });
                }

                List<GenderList> glist = _masterService.GetGenderList();

                // ----------------------------------------------------
                // ENCRYPT RESPONSE
                // ----------------------------------------------------
                var Genderdata = glist.Select(x => new GenderList
                {
                    gender_code = x.gender_code,
                    gender_name = Aes256CbcEncrypterApp.Encrypt(x.gender_name ?? "", key),
                    gender_name_hn = Aes256CbcEncrypterApp.Encrypt(x.gender_name_hn ?? "", key)
                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = Genderdata
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

        // ============================================================
        // GET Age Group List
        // ============================================================
        [HttpPost("GetAgeGroupList")]
        public IActionResult GetAgeGroupList([FromBody] MasterDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ClientKey))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request."
                    });
                }

                // ----------------------------------------------------
                // RSA DECRYPT CLIENT KEY
                // ----------------------------------------------------
                string key = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid client key."
                    });
                }

                List<AgeGroupList> aglist = _masterService.GetAgeGroupList();

                // ----------------------------------------------------
                // ENCRYPT RESPONSE
                // ----------------------------------------------------
                var AgeGroupdata = aglist.Select(x => new AgeGroupList
                {
                    id = x.id,
                    age_group_name = Aes256CbcEncrypterApp.Encrypt(x.age_group_name ?? "", key),
                    age_group_name_hn = Aes256CbcEncrypterApp.Encrypt(x.age_group_name_hn ?? "", key)
                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = AgeGroupdata
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

        // ============================================================
        // GET District List
        // ============================================================
        [HttpPost("GetDistrictList")]
        public IActionResult GetDistrictList([FromBody] MasterDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ClientKey))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request."
                    });
                }

                // ----------------------------------------------------
                // RSA DECRYPT CLIENT KEY
                // ----------------------------------------------------
                string key = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid client key."
                    });
                }

                List<DistrictList> dlist = _masterService.GetDistrictList();

                // ----------------------------------------------------
                // ENCRYPT RESPONSE
                // ----------------------------------------------------
                var Districtdata = dlist.Select(x => new DistrictList
                {
                    District_Code = Aes256CbcEncrypterApp.Encrypt(x.District_Code ?? "", key),
                    District_Name = Aes256CbcEncrypterApp.Encrypt(x.District_Name ?? "", key),
                    District_Name_Hn = Aes256CbcEncrypterApp.Encrypt(x.District_Name_Hn ?? "", key),
                   
                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = Districtdata
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

        // ============================================================
        // GET Block List by District Code
        // ============================================================
        [HttpPost("GetBlockList")]
        public IActionResult GetBlockList([FromBody] BlockListRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ClientKey) || string.IsNullOrWhiteSpace(request.District_Code))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request."
                    });
                }

                // ----------------------------------------------------
                // RSA DECRYPT CLIENT KEY
                // ----------------------------------------------------
                string key = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid client key."
                    });
                }

                // ----------------------------------------------------
                // AES DECRYPT USER ID
                // ----------------------------------------------------
                string DistCode = Aes256CbcEncrypterApp.Decrypt(request.District_Code, key);

                if (string.IsNullOrWhiteSpace(DistCode))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid District Code."
                    });
                }

                // ----------------------------------------------------
                // DATABASE
                // ----------------------------------------------------


                List<BlockList> blist = _masterService.GetBlockList(DistCode);

                if (blist == null)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Block not found."
                    });
                }

                // ----------------------------------------------------
                // ENCRYPT RESPONSE
                // ----------------------------------------------------
                var Blockdata = blist.Select(x => new BlockList
                {
                    BlockCode = Aes256CbcEncrypterApp.Encrypt(x.BlockCode ?? "", key),

                    BlockName = Aes256CbcEncrypterApp.Encrypt(x.BlockName ?? "", key),

                    BlockNameHN = Aes256CbcEncrypterApp.Encrypt(x.BlockNameHN ?? "", key)
                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = Blockdata
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

        // ============================================================
        // GET Panchayat List by Block Code
        // ============================================================
        [HttpPost("GetPanchayatList")]
        public IActionResult GetPanchayatList([FromBody] PanchayatListRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ClientKey) || string.IsNullOrWhiteSpace(request.BlockCode))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request."
                    });
                }

                // ----------------------------------------------------
                // RSA DECRYPT CLIENT KEY
                // ----------------------------------------------------
                string key = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid client key."
                    });
                }

                // ----------------------------------------------------
                // AES DECRYPT USER ID
                // ----------------------------------------------------
                string BlockCode = Aes256CbcEncrypterApp.Decrypt(request.BlockCode, key);

                if (string.IsNullOrWhiteSpace(BlockCode))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid Block Code."
                    });
                }

                // ----------------------------------------------------
                // DATABASE
                // ----------------------------------------------------


                List<PanchayatList> plist = _masterService.GetPanchayatList(BlockCode);

                if (plist == null)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Panchayat not found."
                    });
                }

                // ----------------------------------------------------
                // ENCRYPT RESPONSE
                // ----------------------------------------------------
                var Panchayatdata = plist.Select(x => new PanchayatList
                {
                    PanchayatCode = Aes256CbcEncrypterApp.Encrypt(x.PanchayatCode ?? "", key),
                    PanchayatName = Aes256CbcEncrypterApp.Encrypt(x.PanchayatName ?? "", key),
                    PanchayatNameHnd = Aes256CbcEncrypterApp.Encrypt(x.PanchayatNameHnd ?? "", key),
                    AreaType = Aes256CbcEncrypterApp.Encrypt(x.AreaType ?? "", key)

                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = Panchayatdata
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

        // ============================================================
        // GET Village List by Block Code
        // ============================================================
        [HttpPost("GetVillageList")]
        public IActionResult GetVillageList([FromBody] VillageListRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ClientKey) || string.IsNullOrWhiteSpace(request.PanchayatCode))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request."
                    });
                }

                // ----------------------------------------------------
                // RSA DECRYPT CLIENT KEY
                // ----------------------------------------------------
                string key = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid client key."
                    });
                }

                // ----------------------------------------------------
                // AES DECRYPT USER ID
                // ----------------------------------------------------
                string PanchayatCode = Aes256CbcEncrypterApp.Decrypt(request.PanchayatCode, key);

                if (string.IsNullOrWhiteSpace(PanchayatCode))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid Panchayat Code."
                    });
                }

                // ----------------------------------------------------
                // DATABASE
                // ----------------------------------------------------


                List<VillageList> plist = _masterService.GetVillageList(PanchayatCode);

                if (plist == null)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Panchayat not found."
                    });
                }

                // ----------------------------------------------------
                // ENCRYPT RESPONSE
                // ----------------------------------------------------
                var Villagedata = plist.Select(x => new VillageList
                {
                    VILLCODE = Aes256CbcEncrypterApp.Encrypt(x.VILLCODE ?? "", key),
                    VILLNAME = Aes256CbcEncrypterApp.Encrypt(x.VILLNAME ?? "", key)

                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = Villagedata
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

        // ============================================================
        // GET Ward List by Panchayat Code
        // ============================================================
        [HttpPost("GetWardList")]
        public IActionResult GetWardList([FromBody] WardListRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ClientKey) || string.IsNullOrWhiteSpace(request.PanchayatCode))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request."
                    });
                }

                // ----------------------------------------------------
                // RSA DECRYPT CLIENT KEY
                // ----------------------------------------------------
                string key = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid client key."
                    });
                }

                // ----------------------------------------------------
                // AES DECRYPT USER ID
                // ----------------------------------------------------
                string PanchayatCode = Aes256CbcEncrypterApp.Decrypt(request.PanchayatCode, key);

                if (string.IsNullOrWhiteSpace(PanchayatCode))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid Panchayat Code."
                    });
                }

                // ----------------------------------------------------
                // DATABASE
                // ----------------------------------------------------


                List<WardList> wlist = _masterService.GetWardList(PanchayatCode);

                if (wlist == null)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Panchayat not found."
                    });
                }

                // ----------------------------------------------------
                // ENCRYPT RESPONSE
                // ----------------------------------------------------
                var Warddata = wlist.Select(x => new WardList
                {
                    WARDCODE = Aes256CbcEncrypterApp.Encrypt(x.WARDCODE ?? "", key),
                    WARDNAME = Aes256CbcEncrypterApp.Encrypt(x.WARDNAME ?? "", key)

                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = Warddata
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

        // ============================================================
        // GET Category List
        // ============================================================
        [HttpPost("GetCategoryList")]
        public IActionResult GetCategoryList([FromBody] MasterDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ClientKey))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request."
                    });
                }

                // ----------------------------------------------------
                // RSA DECRYPT CLIENT KEY
                // ----------------------------------------------------
                string key = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid client key."
                    });
                }

                List<CategoryList> clist = _masterService.GetCategoryList();

                // ----------------------------------------------------
                // ENCRYPT RESPONSE
                // ----------------------------------------------------
                var Categorydata = clist.Select(x => new CategoryList
                {
                    category_id = x.category_id,
                    category_name = Aes256CbcEncrypterApp.Encrypt(x.category_name ?? "", key),
                    category_name_hn = Aes256CbcEncrypterApp.Encrypt(x.category_name_hn ?? "", key),
                    is_active = Aes256CbcEncrypterApp.Encrypt(x.is_active ?? "", key)
                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = Categorydata
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

        // ============================================================
        // GET Qualification List
        // ============================================================
        [HttpPost("GetQualificationList")]
        public IActionResult GetQualificationList([FromBody] MasterDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ClientKey))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request."
                    });
                }

                // ----------------------------------------------------
                // RSA DECRYPT CLIENT KEY
                // ----------------------------------------------------
                string key = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid client key."
                    });
                }

                List<QualificationList> qlist = _masterService.GetQualificationList();

                // ----------------------------------------------------
                // ENCRYPT RESPONSE
                // ----------------------------------------------------
                var Qualificationdata = qlist.Select(x => new QualificationList
                {
                    qualification_id = x.qualification_id,
                    qualification_name = Aes256CbcEncrypterApp.Encrypt(x.qualification_name ?? "", key),
                    qualification_name_hn = Aes256CbcEncrypterApp.Encrypt(x.qualification_name_hn ?? "", key),
                    is_active = Aes256CbcEncrypterApp.Encrypt(x.is_active ?? "", key)
                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = Qualificationdata
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

        // ============================================================
        // GET Occupation List
        // ============================================================
        [HttpPost("GetOccupationList")]
        public IActionResult GetOccupationList([FromBody] MasterDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ClientKey))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request."
                    });
                }

                // ----------------------------------------------------
                // RSA DECRYPT CLIENT KEY
                // ----------------------------------------------------
                string key = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid client key."
                    });
                }

                List<OccupationList> olist = _masterService.GetOccupationList();

                // ----------------------------------------------------
                // ENCRYPT RESPONSE
                // ----------------------------------------------------
                var Occupationdata = olist.Select(x => new OccupationList
                {
                    occupation_id = x.occupation_id,
                    occupation_name = Aes256CbcEncrypterApp.Encrypt(x.occupation_name ?? "", key),
                    occupation_name_hn = Aes256CbcEncrypterApp.Encrypt(x.occupation_name_hn ?? "", key),
                    is_active = Aes256CbcEncrypterApp.Encrypt(x.is_active ?? "", key)
                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = Occupationdata
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

        // ============================================================
        // GET Interest List
        // ============================================================
        [HttpGet("GetInterestList")]
        [HttpPost("GetInterestList")]
        public IActionResult GetInterestList([FromBody] MasterDto? request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ClientKey))
                {
                    List<InterestList> rawList = _masterService.GetInterestList();
                    return Ok(new
                    {
                        status = true,
                        message = "Success",
                        data = rawList
                    });
                }

                // ----------------------------------------------------
                // RSA DECRYPT CLIENT KEY
                // ----------------------------------------------------
                string key = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid client key."
                    });
                }

                List<InterestList> ilist = _masterService.GetInterestList();

                // ----------------------------------------------------
                // ENCRYPT RESPONSE
                // ----------------------------------------------------
                var Interestdata = ilist.Select(x => new InterestList
                {
                    interest_id = x.interest_id,
                    interest_name = Aes256CbcEncrypterApp.Encrypt(x.interest_name ?? "", key),
                    interest_name_hn = Aes256CbcEncrypterApp.Encrypt(x.interest_name_hn ?? "", key),
                    is_active = Aes256CbcEncrypterApp.Encrypt(x.is_active ?? "", key)
                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = Interestdata
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

        // ============================================================
        // Get App Version
        // ============================================================

        [HttpGet("GetAppVersion")]
        [AllowAnonymous]
        public IActionResult GetAppVersion()
        {
            var result = _masterService.GetAppVersion();
            return Ok(result);
        }

        // ============================================================
        // Get Public Key
        // ============================================================

        [HttpGet("GetPublicKey")]
        [AllowAnonymous]
        public IActionResult GetPublicKey()
        {
            var result = _masterService.GetPublicKey();
            return Ok(result);
        }       


        // ============================================================
        // GET ALL Portal
        // ============================================================
        [HttpPost("GetPortalList")]
        public IActionResult GetPortalList([FromBody] MasterDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ClientKey))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request."
                    });
                }

                // ----------------------------------------------------
                // RSA DECRYPT CLIENT KEY
                // ----------------------------------------------------
                string key = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(key))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid client key."
                    });
                }

                List<PortalDto> plist = _masterService.GetPortalList();

                // ----------------------------------------------------
                // ENCRYPT RESPONSE
                // ----------------------------------------------------
                var Portaldata = plist.Select(x => new PortalDto
                {                    
                    PortalId = x.PortalId,
                    PortalName = Aes256CbcEncrypterApp.Encrypt(x.PortalName ?? "", key),
                    PortalUrl = Aes256CbcEncrypterApp.Encrypt(x.PortalUrl ?? "", key),
                    PortalImagePath = Aes256CbcEncrypterApp.Encrypt(x.PortalImagePath ?? "", key),
                    IsActive = Aes256CbcEncrypterApp.Encrypt(x.IsActive ?? "", key)
                }).ToList();

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = Portaldata
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An error occurred."
                });
            }
        }

      
    }
}
