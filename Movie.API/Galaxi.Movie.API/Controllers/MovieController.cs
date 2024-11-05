﻿using Galaxi.Bus.Message;
using Galaxi.Movie.Data.Models;
using Galaxi.Movie.Domain.DTOs;
using Galaxi.Movie.Domain.Infrastructure.Commands;
using Galaxi.Movie.Domain.Response;
using Galaxi.Movie.Persistence.Repositorys;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Galaxi.Movie.API.Controllers
{
    [Route("[action]")]
    [ApiController]
    public class MovieController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IBus _bus;
        private readonly IMovieRepository _repo;
        private readonly ILogger<MovieController> _log;

        public MovieController(IMovieRepository repo,
            ILogger<MovieController> log,
            IMediator mediator,
              IBus bus)
        {
            _mediator = mediator;
            _bus = bus;
            _repo = repo;
            _log = log;
        }

        [HttpPost]
        public async Task<IActionResult> migrate()
        {
            await _repo.MigrateAsync();
            IEnumerable<Film> movies = await _repo.GetAllMoviesAsync();
            await _bus.Publish(new MigrationMovies { Movies = movies });

            var successResponse = ResponseHandler<string>.SuccessResponse("DB has been migrated successfully", null);
            return StatusCode(successResponse.StatusCode.Value, successResponse);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMovie([FromBody] CreatedMovieCommand movieToCreate)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                var errorResponse = ResponseHandler<string>.ErrorResponse("Validation failed", errors);
                _log.LogWarning("The model is not valid for creating a movie.", errorResponse);
                return StatusCode(errorResponse.StatusCode.Value, errorResponse);
            }
            try
            {
                _log.LogDebug("The creation of the movie is starting.");
                var filmId = await _mediator.Send(movieToCreate);
                var successResponse = ResponseHandler<CreatedFilmReponseDTO>.SuccessResponse("Movie created successfully", filmId);
                _log.LogInformation($"Successfully processed MovieCreated event for Movie: {movieToCreate.Title}");
                return StatusCode(successResponse.StatusCode.Value, successResponse);
            }
            catch (InvalidOperationException ex)
            {
                _log.LogWarning(ex.Message);
                var errorResponse = ResponseHandler<string>.ErrorResponse("Failed to save changes to the database.", ex);
                return StatusCode(errorResponse.StatusCode.Value, errorResponse);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message);
                var errorResponse = ResponseHandler<string>.ErrorResponse("An internal server error occurred", ex);
                return StatusCode(errorResponse.StatusCode.Value, errorResponse);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMovie(Guid id, [FromBody] UpdateMovieCommand updateMovie)
        {
            if (id != updateMovie.FilmId)
            {
                var errorResponse = ResponseHandler<string>.ErrorResponse("An internal server error occurred",new List<string> {"The movie ID does not match the film ID." });
                return StatusCode(errorResponse.StatusCode.Value, errorResponse);
            }
            try
            {
                _log.LogDebug("The update of the movie is starting.");
                var Update = await _mediator.Send(updateMovie);
                var successResponse = ResponseHandler<UpdateMovieCommand>.SuccessResponse("Movie updated successfully", updateMovie);
                _log.LogInformation($"Successfully processed update event for Movie: {updateMovie.Title}");
                return StatusCode(successResponse.StatusCode.Value, successResponse);
            }
            catch (KeyNotFoundException ex)
            {
                _log.LogWarning(ex.Message);
                var response = ResponseHandler<string>.NotFoundResponse("Movie not found.", "The movie with the specified ID does not exist.");
                return StatusCode(response.StatusCode.Value, response);
            }
            catch (InvalidOperationException ex)
            {
                _log.LogWarning(ex.Message);
                var errorResponse = ResponseHandler<string>.ErrorResponse("Failed to save changes to the database.", ex);
                return StatusCode(errorResponse.StatusCode.Value, errorResponse);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message);
                var errorResponse = ResponseHandler<string>.ErrorResponse("An internal server error occurred", ex);
                return StatusCode(errorResponse.StatusCode.Value, errorResponse);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMovie(Guid id)
        {
            DeleteMovieCommand FilmId = new DeleteMovieCommand(FilmId: id);
            try
            {
                _log.LogDebug("The delete of the movie is starting.");
                var delete = await _mediator.Send(FilmId);
                var successResponse = ResponseHandler<string>.SuccessResponse("Movie deleted successfully", null);
                _log.LogInformation("Movie deleted successfully");
                return StatusCode(successResponse.StatusCode.Value, successResponse);
            }
            catch (KeyNotFoundException ex)
            {
                _log.LogWarning(ex.Message);
                var response = ResponseHandler<string>.NotFoundResponse("Movie not found.", "The movie with the specified ID does not exist.");
                return StatusCode(response.StatusCode.Value, response);
            }
            catch (InvalidOperationException ex)
            {
                _log.LogWarning(ex.Message);
                var errorResponse = ResponseHandler<string>.ErrorResponse("Failed to save changes to the database.", ex);
                return StatusCode(errorResponse.StatusCode.Value, errorResponse);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message);
                var errorResponse = ResponseHandler<string>.ErrorResponse("An internal server error occurred", ex);
                return StatusCode(errorResponse.StatusCode.Value, errorResponse);
            }
        }
    }
}
