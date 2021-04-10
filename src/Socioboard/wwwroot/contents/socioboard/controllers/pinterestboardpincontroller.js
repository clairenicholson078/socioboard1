﻿'use strict';

SocioboardApp.controller('PinterestBoardPinsController', function ($rootScope, $scope, $http, $timeout, $mdpDatePicker, $mdpTimePicker, $stateParams, apiDomain) {
    //alert('helo');
    $scope.$on('$viewContentLoaded', function () {

        pinterest();
        var start = 0; // where to start data
        var ending = start + 30; // how much data need to add on each function call
        var reachLast = false; // to check the page ends last or not
        var count = 30;
        $scope.check = false;
        if ($rootScope.user.TrailStatus == 2) {
            count = 5
        }
        $scope.LoadTopFeeds = function () {
            //codes to load  recent Feeds
            $http.get(apiDomain + '/api/Pinterest/GetTopBoardPins?boardId=' + $stateParams.profileId + '&userId=' + $rootScope.user.Id + '&skip=0&take=' + count)
                              .then(function (response) {
                                  $scope.lstpin = response.data;
                              }, function (reason) {
                                  $scope.error = reason.data;
                              });
            // end codes to load  recent Feeds

        }
        $scope.LoadTopFeeds();

        $scope.CheckStatus = function () {
            var web_url = $('#web_url').val();
            if (web_url != "") {
                if ((/\.(gif|jpg|jpeg|tiff|png)$/i).test(web_url)) {

                } else {
                    swal('please upload any valid image url for pin');
                    return false;
                }
            } else {
                $scope.checkfile();
                if ($scope.check == false) {
                    alertify.set({ delay: 3000 });
                    $('#composePinImage').val('');
                    alertify.error("File extension is not valid. Please upload an image file");
                } else {
                }
            }

        }


        $scope.CheckNextStatus = function () {
            var web_url = $('#web_url').val();
            if (web_url != "") {
                if ((/\.(gif|jpg|jpeg|tiff|png)$/i).test(web_url)) {
                    $('#CreatePinModal').closeModal();
                    $('#SavePinModal').openModal();
                } else {
                    swal('please upload any valid image url for pin');

                }
            } else {
                swal('please upload any valid image url for pin');
            }
        }

        $scope.CheckNextStatusBrowse = function () {
            $scope.checkfile();
            if ($scope.check == false) {
                alertify.set({ delay: 3000 });
                $('#composePinImage').val('');
                alertify.error("File extension is not valid. Please upload an image file");
            } else {
                $('#CreatePinModal').closeModal();
                $('#SavePinModal').openModal();
            }
        }

        $scope.CreatePin = function (pinterestUserId, boardid) {
            var web_url = $('#web_url').val();
            if (web_url != "") {
                if ((/\.(gif|jpg|jpeg|tiff|png)$/i).test(web_url)) {

                } else {
                    swal('please upload any valid image url for pin');
                    return false;
                }
            }
            var dec = $('#pinboard').val();
            if (web_url == "" && $("#composePinImage").get(0).files[0] == undefined) {
                swal('please upload any image or image url for pin');
                return false;
            }
            else {
                $('#pinbtn_' + boardid).html('wait');
                var description = dec;
                if ($scope.check == true) {
                    var formData = new FormData();
                    formData.append('files', $("#composePinImage").get(0).files[0]);
                    $http({
                        method: 'POST',
                        url: apiDomain + '/api/Pinterest/CreateUserPins?pinterestUserId=' + pinterestUserId + '&boardId=' + boardid + '&note=' + encodeURIComponent(description) + '&userId=' + $rootScope.user.Id + '&filepath=' + web_url,
                        data: formData,
                        headers: {
                            'Content-Type': undefined
                        },
                        transformRequest: angular.identity,
                    }).then(function (response) {
                        $('#pinbtn_' + boardid).html('saved');
                        swal(response.data);
                        $('#CreatePinModal').closeModal();
                        $scope.LoadTopFeeds();
                    }, function (reason) {
                        $scope.error = reason.data;
                    });
                } else {
                    alertify.set({ delay: 3000 });
                    alertify.error("File extension is not valid. Please upload an image file");

                }
            }
        }

        //code for checking the file format start
        $scope.checkfile = function () {
            var filesinput = $('#composePinImage');//composeImage
            var fileExtension = ['jpeg', 'jpg', 'png', 'gif', 'bmp', 'mov', 'mp4', 'mpeg', 'wmv', 'avi', 'flv', '3gp'];
            if (filesinput != undefined && filesinput[0].files[0] != null) {
                if ($scope.hasExtension('#composePinImage', fileExtension)) {
                    $scope.check = true;
                }
                else {
                    $scope.check = false;
                }
            }
            else {
                $scope.check = false;
            }
        }
        $scope.hasExtension = function (inputID, exts) {
            var fileName = $('#composePinImage').val();
            return (new RegExp('(' + exts.join('|').replace(/\./g, '\\.') + ')$')).test(fileName);
        }
        //code for checking the file format end


        /*
           * Masonry container for eCommerce page
           */
        //var $containerProducts = $("#pins_board");
        //$containerProducts.imagesLoaded(function() {
        //  $containerProducts.masonry({
        //    itemSelector: ".product",
        //    columnWidth: ".product-sizer",
        //  });
        //});

    });

});
SocioboardApp.directive('myRepeatFeedTimeoutDirective', function ($timeout) {
    return function (scope, element, attrs) {
        if (scope.$last === true) {
            $timeout(function () {
                console.log("myRepeatFeedTimeoutDirective Called");
                var $containerProducts = $("#pins_board");
                $containerProducts.masonry({
                    itemSelector: ".product",
                    columnWidth: ".product-sizer",
                });
            });


        };
    }
});