using System.ComponentModel.DataAnnotations;
using Api.Dto;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BaseController : ControllerBase
{
}