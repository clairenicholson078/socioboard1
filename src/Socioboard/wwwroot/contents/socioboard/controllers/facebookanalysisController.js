'use strict';

SocioboardApp.controller('facebookanalysisController', function ($rootScope, $scope, $http, $timeout, apiDomain, $state) {
    //alert('helo');
    $scope.$on('$viewContentLoaded', function() {   
    

        var start = 0; // where to start data
        var ending = 0; // how much data need to add on each function call
        var reachLast = false; // to check the page ends last or not
        $scope.dispbtn = false;
        //$scope.loadmore = "/contents/socioboard/images/loader.gif";
        $rootScope.categories = '';
        $scope.continue = true;
        designfeeds();
      
        $scope.deleteProfile = function(profileId){
        	// console.log(profileId);
        	swal({   
	        title: "Are you sure?",   
	        text: "You will not be able to send message via this account!",   
	        type: "warning",   
	        showCancelButton: true,   
	        confirmButtonColor: "#DD6B55",   
	        confirmButtonText: "Yes, delete it!",   
	        closeOnConfirm: false }, 
	        function(){   
	            //todo: code to delete profile
	            swal("Deleted!", "Your profile has been deleted.", "success"); 
	            });
        }


        $scope.discovery = function (keyword) {
            var categories = $('#categories').val();
            if (categories == '') {
                categories = keyword;
            }
            if (categories != "" && categories != undefined) {
                $rootScope.categories = categories;
                //codes to load facebook discovery start
                $http.post(apiDomain + '/api/FacebookGroups/GetFacebookGroupFeeds?skip='+ending + '&count=30' + '&keyword=' + categories)
                              .then(function (response) {
                                  if (response.data!="") {
                                      $('#categories').val('');
                                      $('#searchcatagory').closeModal();
                                      if (response.data == null) {
                                          reachLast = true;
                                      } else {
                                      $scope.SearchFacebookdate(response.data);
                                      ending = ending + 30;
                                      
                                      }
                                      
                                  }
                                  else {
                                      $('#categories').val('');
                                      swal('no post found');
                                  }
                              }, function (reason) {
                                  $scope.error = reason.data;
                              });
                // end codes to load facebook discovery
            }
            else {
                swal('please enter any keyword');
            }
        }

        $scope.GetCategories = function () {

            $http.get(apiDomain + '/api/FacebookGroups/GetCategories')
                              .then(function (response) {
                                  $scope.lstcategories = response.data;
                              }, function (reason) {
                                  $scope.error = reason.data;
                              });
            
        }


        $scope.discoveryList = function () {
            $scope.dispbtn = true;
          
            var categories = $rootScope.categories;
            if (categories != "" && categories != undefined) {
                $rootScope.categories = categories;
                //codes to load facebook discovery start
                $http.post(apiDomain + '/api/FacebookGroups/GetFacebookGroupFeeds?skip=' + ending + '&count=30' + '&keyword=' + categories)
                              .then(function (response) {
                                  if (response.data != "") {
                                      if (response.data == null) {
                                          reachLast = true;
                                          $scope.continue = true;
                                          $scope.dispbtn = false;
                                         
                                      } else {
                                          $scope.SearchFacebookdate(response.data);
                                          $scope.dispbtn = true;
                                          $scope.continue = true;
                                          ending = ending + 30;
                                      }
                                  }
                                  else {
                                      reachLast = true;
                                      $scope.continue = false;
                                      $scope.dispbtn = false;
                                     // $scope.loadmore = 'reached at bottom';
                                  }
                              }, function (reason) {
                                  $scope.error = reason.data;
                              });
                // end codes to load facebook discovery
            }
            else {
                swal('please enter any keyword');
            }
        }

        $scope.SearchFacebookdate = function (parm) {

            for (var i = 0; i < parm.length; i++) {
                var date = moment(parm[i].DateTimeOfPost);
                var newdate = date.toString();
                var splitdate = newdate.split(" ");
                date = splitdate[0] + " " + splitdate[1] + " " + splitdate[2] + " " + splitdate[3];
                parm[i].DateTimeOfPost = date;
            }
            $scope.lstDiscoverySearchFacebook= parm;

        }


        $scope.schedulefbpost = function (schedulemessage) {
            $rootScope.schedulemessage = schedulemessage;
            $rootScope.grppost = true;
            //window.location.href = "#/schedulemessage";
           $state.go('schedulemessage');
        }

        $scope.GetCategories();
        $('#searchcatagory').openModal();

        // vanilla JS
		// init with element
		var grid = document.querySelector('.grid');
		var msnry = new Masonry( grid, {
		  // options...
		  itemSelector: '.grid-item',
		  columnWidth: 200
		});

		// init with selector
		var msnry = new Masonry( '.grid', {
		  // options...
		});

    });

});