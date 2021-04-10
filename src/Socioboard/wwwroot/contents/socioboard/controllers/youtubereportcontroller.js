'use strict';

SocioboardApp.controller('YoutubereportController', function ($rootScope, $scope, $http, $timeout, $stateParams, apiDomain, domain) {
    //alert('helo');
    $scope.$on('$viewContentLoaded', function() {   
        $scope.loadingYAData = 'open';
        $scope.loadedYAData = "hide";
        youtubereport();

        $scope.chartData = [];
        $scope.graphData = [];

        $scope.generateChartData = function (days) {
            $scope.chartData = [];
            var startDate = new Date((Date.now() - (days * 86400000))) / 1000;
            //alert("Rajsekhar");
            angular.forEach($scope.dailyYReportsList, function (value, key) {
                if (value.datetime_unix > startDate) {
                    $scope.chartData.push({
                        date: new Date((value.datetime_unix * 1000)),
                        commentss: value.comments,
                        likess: value.likes,
                        viewss: value.views,
                        subscriberss: value.subscribersGained,
                    });
                }
            });


        }


        //Chart Start
        
        $scope.generateGraph = function (days) {
            $scope.generateChartData(days);
            

            var chart = AmCharts.makeChart("chartdiv_subscribersTotal", {
                "type": "serial",
                "theme": "light",
                "marginRight": 80,
                "autoMarginOffset": 20,
                "marginTop": 7,
                "dataProvider": $scope.chartData,
                "valueAxes": [{
                    "axisAlpha": 0.2,
                    "dashLength": 2.2,
                    "position": "left"
                }],
                "graphs": [{
                    "id": "g1",
                    "balloonText": "S:" + "[[value]]",
                    "bullet": "round",
                    "lineColor": "#bb0000",
                    "bulletBorderAlpha": 1,
                    "hideBulletsCount": 60,
                    "title": "red line",
                    "lineThickness": 2.1,
                    "bulletSize": 5,
                    "valueField": "subscriberss",
                    "useLineColorForBulletBorder": true,
                    "balloon": {
                        "drop": true
                    }
                    
                }],
                "chartScrollbar": {
                    "autoGridCount": true,
                    "graph": "g1",
                    "scrollbarHeight": 40
                },
                "chartCursor": {
                    "limitToGraph": "g1"
                },
                "categoryField": "date",
                "categoryAxis": {
                    "parseDates": true,
                    "axisColor": "#DADADA",
                    "dashLength": 5,
                    "minorGridEnabled": true
                },
                "export": {
                    "enabled": true
                }
            });


            var chart = AmCharts.makeChart("chartdiv_comment", {
                "type": "serial",
                "theme": "light",
                "marginRight": 80,
                "autoMarginOffset": 20,
                "marginTop": 7,
                "dataProvider": $scope.chartData,
                "valueAxes": [{
                    "axisAlpha": 0.2,
                    "dashLength": 2.2,
                    "position": "left"
                }],
                "graphs": [{
                    "id": "g1",
                    "balloonText": "C:" + "[[value]]",
                    "bullet": "round",
                    "lineColor": "#2a63bf",
                    "bulletBorderAlpha": 1,
                    "hideBulletsCount": 60,
                    "title": "red line",
                    "lineThickness": 2.1,
                    "bulletSize": 5,
                    "valueField": "commentss",
                    "useLineColorForBulletBorder": true,
                    "balloon": {
                        "drop": true
                    }
                    
                }],
                "chartScrollbar": {
                    "autoGridCount": true,
                    "graph": "g1",
                    "scrollbarHeight": 40
                },
                "chartCursor": {
                    "limitToGraph": "g1"
                },
                "categoryField": "date",
                "categoryAxis": {
                    "parseDates": true,
                    "axisColor": "#DADADA",
                    "dashLength": 5,
                    "minorGridEnabled": true
                },
                "export": {
                    "enabled": true
                }
            });

            var chart = AmCharts.makeChart("chartdiv_like", {
                "type": "serial",
                "theme": "light",
                "marginRight": 80,
                "autoMarginOffset": 20,
                "marginTop": 7,
                "dataProvider": $scope.chartData,
                "valueAxes": [{
                    "axisAlpha": 0.2,
                    "dashLength": 2.2,
                    "position": "left"
                }],
                "graphs": [{
                    "id": "g1",
                    "balloonText": "L:" + "[[value]]",
                    "bullet": "round",
                    "lineColor": "#E55B00",
                    "bulletBorderAlpha": 1,
                    "hideBulletsCount": 60,
                    "title": "red line",
                    "lineThickness": 2.1,
                    "bulletSize": 5,
                    "valueField": "likess",
                    "useLineColorForBulletBorder": true,
                    "balloon": {
                        "drop": true
                    }

                }],
                "chartScrollbar": {
                    "autoGridCount": true,
                    "graph": "g1",
                    "scrollbarHeight": 40
                },
                "chartCursor": {
                    "limitToGraph": "g1"
                },
                "categoryField": "date",
                "categoryAxis": {
                    "parseDates": true,
                    "axisColor": "#DADADA",
                    "dashLength": 5,
                    "minorGridEnabled": true
                },
                "export": {
                    "enabled": true
                }
            });

            var chart = AmCharts.makeChart("chartdiv_view", {
                "type": "serial",
                "theme": "light",
                "marginRight": 80,
                "autoMarginOffset": 20,
                "marginTop": 7,
                "dataProvider": $scope.chartData,
                "valueAxes": [{
                    "axisAlpha": 0.2,
                    "dashLength": 2.2,
                    "position": "left"
                }],
                "graphs": [{
                    "id": "g1",
                    "balloonText": "V:" + "[[value]]",
                    "bullet": "round",
                    "lineColor": "#1e8e12",
                    "bulletBorderAlpha": 1,
                    "hideBulletsCount": 60,
                    "title": "red line",
                    "lineThickness": 2.1,
                    "bulletSize": 5,
                    "valueField": "viewss",
                    "useLineColorForBulletBorder": true,
                    "balloon": {
                        "drop": true
                    }

                }],
                "chartScrollbar": {
                    "autoGridCount": true,
                    "graph": "g1",
                    "scrollbarHeight": 40
                },
                "chartCursor": {
                    "limitToGraph": "g1"
                },
                "categoryField": "date",
                "categoryAxis": {
                    "parseDates": true,
                    "axisColor": "#DADADA",
                    "dashLength": 5,
                    "minorGridEnabled": true
                },
                "export": {
                    "enabled": true
                }
            });
            
            //chart.addListener("rendered", zoomChart);
            //zoomChart();

            //// this method is called when chart is first inited as we listen for "rendered" event
            //function zoomChart() {
            //    // different zoom methods can be used - zoomToIndexes, zoomToDates, zoomToCategoryValues
            //    chart.zoomToIndexes(chartData.length - 40, chartData.length - 1);
            //}

        }
        

        //Chart End


        $scope.getChartData = function (days) {
            $scope.lastxDays = days;
            var startDate = new Date((Date.now() - (days * 86400000))) / 1000;
            var endDate = Date.now() / 1000;
            var totalLikes = 0;
            var totalComments = 0;
            var totalViews = 0;
            var totalSubscribers = 0;
            
            $scope.graphData = [];
           
            angular.forEach($scope.dailyYReportsList, function (value, key) {
                if (value.datetime_unix > startDate) {
                    totalLikes = totalLikes + value.likes;
                    totalComments = totalComments + value.comments;
                    totalViews = totalViews + value.views;
                    totalSubscribers = totalSubscribers + value.subscribersGained;
                    $scope.graphData = $scope.graphData.concat(value);
                }
            });

            $scope.totalLikess = totalLikes;
            $scope.totalCommentss = totalComments;
            $scope.totalViewss = totalViews;
            $scope.totalSubscriberss = totalSubscribers;

            $scope.fromDate = new Date(startDate * 1000);
            $scope.toDate = new Date(endDate * 1000);
            $scope.generateGraph(days);

        }



        $scope.LoadReportData = function (channelId) {
         
            $scope.rowLimit = 3;
            //$stateParams.profileId
            //codes to load tabledata
            $http.get(apiDomain + '/api/YoutubeReport/GetYtCustomTableData?channelId=' + channelId)
                              .then(function (response) {
                                  $scope.tableData = response.data;
                                
                              }, function (reason) {
                                  $scope.error = reason.data;
                              });
            // end codes to load tabledata

        }
        //$scope.LoadReportData();


        $scope.getReportsChart = function (profileId, days) {
          
            $scope.loadingYAData = 'open';
            $scope.loadedYAData = 'hide';
            //codes to load  reportdata
            $http.get(apiDomain + '/api/YoutubeReport/GetYtReports?ChannelId=' + profileId + '&daysCount=' + days)
                          .then(function (response) {
                              $scope.dailyYReportsList = response.data;
                              $scope.test = "Raj";
                              $scope.loadingYAData = 'hide';
                              $scope.loadedYAData = 'open';
                              $scope.getChartData(days);
                          }, function (reason) {
                              $scope.error = reason.data;
                          });
        }
        // end codes to load reportdata


      
        $scope.getOnPageLoadReports = function () {
            var canContinue = true;
            angular.forEach($rootScope.lstProfiles, function (value, key) {
                if (canContinue && value.profileType == 7) {
                    
                    $scope.getReportsChart(value.profileId, 90)
                    $scope.LoadReportData(value.profileId)
                    $scope.selectedProfile = value.profileId;
                    canContinue = false;
                }
            });
        }


        $scope.getOnPageLoadReports();


        //// generate some random data, quite different range
        //function generateChartData() {
        //    var chartData = [];
        //    var firstDate = new Date();
        //    firstDate.setDate(firstDate.getDate() - 5);

        //    for (var i = 0; i < 90; i++) {
        //        // we create date objects here. In your data, you can have date strings
        //        // and then set format of your dates using chart.dataDateFormat property,
        //        // however when possible, use date objects, as this will speed up chart rendering.
        //        var newDate = new Date(firstDate);
        //        newDate.setDate(newDate.getDate() + i);

        //        var visits = Math.round(Math.random() * (40 + i / 5)) + 20 + i;

        //        chartData.push({
        //            date: newDate,
        //            visits: visits
        //        });
        //    }
        //    return chartData;
        //}

        ////sample Line



  });

});

SocioboardApp.directive('myRepeatTabDirective', function ($timeout) {
    return function (scope, element, attrs) {
        if (scope.$last === true) {
            $timeout(function () {
                $('select').material_select();
            });
        }
    };
})

SocioboardApp.directive('myRepeatVideoTabDirective', function ($timeout) {
    return function (scope, element, attrs) {
        if (scope.$last === true) {
            $timeout(function () {

                //$('#all_video_table').DataTable();
                $('#channeldetailss').DataTable({
                    dom: 'Bfrtip',
                    buttons: [
                        'copy', 'csv', 'excel', 'pdf', 'print'
                    ]
                });
            });
        }
    };
})