using Api.Interfaces;
using Api.Models.DTOs;
using ApplicationLayer;
using AutoMapper;
using CoreLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CourseController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IJwtHandler _jwtHandler;
        private readonly IUnitOfWork _unitOfWork;

        public CourseController(IMapper mapper, IUnitOfWork unitOfWork, IJwtHandler jwtHandler)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _jwtHandler = jwtHandler;
        }

        [HttpPost]
        //[Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStudentCoursesByStudentId([FromBody] Guid? studentId)
        {
            var studentCourses = await _unitOfWork._studentCourseRepository.GetAllAsync();
            var courses = await _unitOfWork._courseRepository.GetAllAsync();
            var categories = await _unitOfWork._categoryRepository.GetAllAsync();
            var teachers = await _unitOfWork._teacherRepository.GetAllAsync();
            var result = studentCourses
                .Where(sc => sc.StudentId.Equals(studentId))
                .Join(courses, sc => sc.CourseId, c => c.CourseId, (sc, c) => new { sc, c })
                .Join(categories, scj => scj.c.CategoryId, c => c.CategoryId, (scj, c) => new { result1 = scj, c })
                .Join(teachers, result2 => result2.result1.c.TeacherId, t => t.TeacherId, (scjcj, t) => (scjcj, t))
                .Select(_ =>
                    _mapper.Map<(Course, Teacher, StudentCourse), StudentCourseDTO>(
                        (_.scjcj.result1.c, _.t, _.scjcj.result1.sc)));
            return Ok(result);
        }

        [HttpPost]
        //[Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTeacherCourses()
        {
            try
            {

                Request.Headers.TryGetValue("Authorization", out var values);
                var accountId = _jwtHandler.GetAccountIdFromJwt(values);
                var account = await _unitOfWork._accountRepository.GetByIdAsync(new Guid(accountId));

                if (account == null)
                {
                    return NotFound("Account not recognized");
                }

                var user = await _unitOfWork._userRepository.GetUserByAccountIdAsync(account.AccountId);

                var teacher = await _unitOfWork._teacherRepository.GetTeacherByUserIdAsync(user.UserId);


                var courses = await _unitOfWork._courseRepository.GetAllAsync();
                var teacherCourses = courses.Where(c => c.TeacherId.Equals(teacher.TeacherId)).Select(_ => _mapper.Map<Course, TeacherCourseDTO>(_));
                if (!teacherCourses.Any())
                {
                    return StatusCode(StatusCodes.Status404NotFound);
                }
                return Ok(teacherCourses);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }

        }

        [HttpDelete]
        //[Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnEnrollStudentCourseById(Guid courseId)
        {
            try
            {
                Request.Headers.TryGetValue("Authorization", out var values);
                var accountId = _jwtHandler.GetAccountIdFromJwt(values);
                var student = await _unitOfWork._accountRepository.GetByIdAsync(new Guid(accountId));

                if (student == null)
                {
                    return NotFound("User associated with this account is not found");
                }

                var studentCourse = await _unitOfWork._studentCourseRepository.GetByIdAsync(courseId);

                if (studentCourse == null)
                {
                    return NotFound("Student was not enroll this course!");
                }

                await _unitOfWork._studentCourseRepository.DeleteAsync(courseId);
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpPost]
        //[Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EnrollStudentCourseById(Guid courseId)
        {
            try
            {
                Request.Headers.TryGetValue("Authorization", out var values);
                var accountId = _jwtHandler.GetAccountIdFromJwt(values);
                var account = await _unitOfWork._accountRepository.GetByIdAsync(new Guid(accountId));

                if (account == null)
                {
                    return NotFound("User associated with this account is not found");
                }

                var studentCourse = await _unitOfWork._studentCourseRepository.GetByIdAsync(courseId);

                if (studentCourse != null)
                {
                    return Conflict("Student already enrolled this course!");
                }

                await _unitOfWork._studentCourseRepository.AddAsync(courseId);
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        //[HttpGet("{name}/{code}")]
        ////[Authorize]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> GetCourseByNameOrCode(string name, string code)
        //{
        //    var courses = await _unitOfWork._courseRepository.GetAllAsync();
        //}


        [HttpGet("{id}")]
        //[Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCourseById(Guid id)
        {
            try
            {
                var course = await _unitOfWork._courseRepository.GetByIdAsync(id);
                if (course == null)
                {
                    return NotFound("Course with this id is not exist");
                }

                var category = await _unitOfWork._categoryRepository.GetByIdAsync(course.CategoryId);
                var result = _mapper.Map<CourseDTO>((course, category));
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetResourceCourseById(Guid id)
        {
            try
            {
                var course = await _unitOfWork._courseRepository.GetByIdAsync(id);
                if (course == null)
                {
                    return NotFound("Course with this id is not exist");
                }

                var category = await _unitOfWork._categoryRepository.GetByIdAsync(course.CategoryId);
                var result = _mapper.Map<CourseDTO>((course, category));
                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}