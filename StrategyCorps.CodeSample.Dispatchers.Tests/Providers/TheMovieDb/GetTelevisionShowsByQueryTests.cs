﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AutoMapper;
using ExpectedObjects;
using FizzWare.NBuilder;
using Moq;
using Newtonsoft.Json;
using NLog;
using NUnit.Framework;
using RestSharp;
using StrategyCorps.CodeSample.Core.Exceptions;
using StrategyCorps.CodeSample.Dispatchers.Providers.TheMovieDB;
using StrategyCorps.CodeSample.Dispatchers.Providers.TheMovieDB.Model;
using StrategyCorps.CodeSample.Models;

namespace StrategyCorps.CodeSample.Dispatchers.Tests.Providers.TheMovieDb
{
    [TestFixture]
    public class GetTelevisionShowsByQueryTests
    {
        private Mock<IRestClient> _restClientMock;
        private Mock<ILogger> _loggerMock;
        private Mock<IMapper> _mapperMock;

        [SetUp]
        public void SetUp()
        {
            _restClientMock = new Mock<IRestClient>();
            _loggerMock = new Mock<ILogger>();
            _mapperMock = new Mock<IMapper>();
        }

        [TearDown]
        public void TearDown()
        {
            _restClientMock = new Mock<IRestClient>();
            _loggerMock = new Mock<ILogger>();
            _mapperMock = new Mock<IMapper>();
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void GetTelevisionShowsByQuery_When_QueryIsNullOrWhitespace_Throws_StrategyCorpsException(string query)
        {
            var theMovieDbDispatcher = new TheMovieDbDispatcher(null, null, null);
            var exception = Assert.Throws<ArgumentNullException>(() => theMovieDbDispatcher.GetTelevisionShowsByQuery(query));
            Assert.AreEqual("query", exception.ParamName);
        }

        [Test]
        [TestCase("Gotham")]
        public void GetTelevisionShowsByQuery_When_RestClientThrowsException_Throws_Exception(string query)
        {
            _loggerMock.Setup(x => x.Error(It.IsAny<Exception>())).Verifiable();
            _restClientMock.Setup(x => x.Execute(It.IsAny<IRestRequest>())).Throws<Exception>().Verifiable();
            var theMovieDbDispatcher = new TheMovieDbDispatcher(_restClientMock.Object, _loggerMock.Object, null);
            Assert.Throws<Exception>(() => theMovieDbDispatcher.GetTelevisionShowsByQuery(query));

            _loggerMock.Verify(x => x.Error(It.IsAny<Exception>()),Times.Once);
            _restClientMock.Verify(x => x.Execute(It.IsAny<IRestRequest>()), Times.Once);
        }

        [Test]
        [TestCase("Gotham")]
        public void GetTelevisionShowsByQuery_When_RestClientReturnsBadRequest_Throws_StrategyCorpException(string query)
        {
            var restResponse = Builder<RestResponse>.CreateNew()
                                                    .With(x => x.StatusCode = HttpStatusCode.BadRequest).Build();
            _loggerMock.Setup(x => x.Error(It.IsAny<StrategyCorpsException>())).Verifiable();
            _restClientMock.Setup(x => x.Execute(It.IsAny<IRestRequest>())).Returns(restResponse).Verifiable();
            var theMovieDbDispatcher = new TheMovieDbDispatcher(_restClientMock.Object, _loggerMock.Object, null);
            Assert.Throws<StrategyCorpsException>(() => theMovieDbDispatcher.GetTelevisionShowsByQuery(query));

            _loggerMock.Verify(x => x.Error(It.IsAny<StrategyCorpsException>()), Times.Once);
            _restClientMock.Verify(x => x.Execute(It.IsAny<IRestRequest>()), Times.Once);
        }

        [Test]
        [TestCase("Gotham")]
        public void GetTelevisionShowsByQuery_When_RestClientReturnsBadGateway_Throws_StrategyCorpException(string query)
        {
            var restResponse = Builder<RestResponse>.CreateNew()
                                                    .With(x => x.StatusCode = HttpStatusCode.BadGateway).Build();
            _loggerMock.Setup(x => x.Error(It.IsAny<StrategyCorpsException>())).Verifiable();
            _restClientMock.Setup(x => x.Execute(It.IsAny<IRestRequest>())).Returns(restResponse).Verifiable();
            var theMovieDbDispatcher = new TheMovieDbDispatcher(_restClientMock.Object, _loggerMock.Object, null);
            Assert.Throws<StrategyCorpsException>(() => theMovieDbDispatcher.GetTelevisionShowsByQuery(query));

            _loggerMock.Verify(x => x.Error(It.IsAny<StrategyCorpsException>()), Times.Once);
            _restClientMock.Verify(x => x.Execute(It.IsAny<IRestRequest>()), Times.Once);
        }

        [Test]
        [TestCase("Gotham")]
        public void GetTelevisionShowsByQuery_When_Successful_Returns_TelevisionSearchResponseDTO(string query)
        {
            var televisionResults = Builder<TelevisionResult>.CreateListOfSize(5).Build().ToList();
            var televisionSearchResponse = Builder<TelevisionSearchResponse>.CreateNew().With(x => x.Results = televisionResults).Build();
            var televisionResultsDTO = televisionResults.Select(televisionResult => new TelevisionResultDTO
            {
                FirstAirDate = televisionResult.FirstAirDate,
                Id = televisionResult.Id,
                Name = televisionResult.Name,
                OriginalLanguage = televisionResult.OriginalLanguage,
                OriginalName = televisionResult.OriginalName,
                Overview = televisionResult.Overview,
                Popularity = televisionResult.Popularity,
                VoteAverage = televisionResult.VoteAverage,
                VoteCount = televisionResult.VoteCount
            }).ToList();

            var expectedResult = Builder<TelevisionSearchResponseDTO>.CreateNew()
                                                                     .With(x => x.Page = televisionSearchResponse.Page)
                                                                     .With(x => x.TotalPages = televisionSearchResponse.TotalPages)
                                                                     .With(x => x.TotalResults = televisionSearchResponse.TotalResults)
                                                                     .With(x => x.Results = televisionResultsDTO).Build();
            var restResponse = Builder<RestResponse>.CreateNew()
                .With(x => x.StatusCode = HttpStatusCode.OK)
                .With(x => x.Content = JsonConvert.SerializeObject(televisionSearchResponse)).Build();
            _loggerMock.Setup(x => x.Error(It.IsAny<Exception>())).Verifiable();
            _restClientMock.Setup(x => x.Execute(It.IsAny<IRestRequest>())).Returns(restResponse).Verifiable();
            _mapperMock.Setup(x => x.Map<TelevisionSearchResponse, TelevisionSearchResponseDTO>(It.IsAny<TelevisionSearchResponse>())).Returns(expectedResult).Verifiable();

            var theMovieDbDispatcher = new TheMovieDbDispatcher(_restClientMock.Object, _loggerMock.Object,_mapperMock.Object);
            var actualResult = theMovieDbDispatcher.GetTelevisionShowsByQuery(query);

            actualResult.ToExpectedObject().ShouldEqual(expectedResult);

            _loggerMock.Verify(x => x.Error(It.IsAny<Exception>()), Times.Never);
            _restClientMock.Verify(x => x.Execute(It.IsAny<IRestRequest>()), Times.Once);
            _mapperMock.Verify(x => x.Map<TelevisionSearchResponse, TelevisionSearchResponseDTO>(It.IsAny<TelevisionSearchResponse>()), Times.Once);

        }
    }
}
