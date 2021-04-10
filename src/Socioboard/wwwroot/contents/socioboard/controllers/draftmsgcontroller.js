﻿'use strict';
SocioboardApp.controller('DraftMessageController', function ($rootScope, $scope, $http, $modal, $timeout, $stateParams, apiDomain,$state,utility) {
    $scope.$on('$viewContentLoaded', function () {
        draft();
        $scope.utility = utility;
        $scope.deleteMsg = function (draftId) {
            swal({
                title: "Are you sure?",
                type: "warning",
                showCancelButton: true,
                confirmButtonColor: "#DD6B55",
                confirmButtonText: "Yes, delete it!",
                closeOnConfirm: false
            },
	        function () {
	            //todo: code to delete profile
	            //codes to delete  draft messages start
	            $http.get(apiDomain + '/api/DraftMessage/DeleteDraftMessage?groupId=' + $rootScope.groupId + '&userId=' + $rootScope.user.Id + '&draftId=' + draftId)
                              .then(function (response) {
                                  closeOnConfirm: false
                                  swal("deleted");
                                  // $scope.lstdraftmessage = response.data;
                                  $scope.date(response.data);
                              }, function (reason) {
                                  $scope.error = reason.data;
                              });
	            // end codes to delete draft messages
	        });
        }


        $scope.deleteafterschdu = function (draftId) {
           
	            //todo: code to delete profile
	            //codes to delete  draft messages start
	            $http.get(apiDomain + '/api/DraftMessage/DeleteDraftMessage?groupId=' + $rootScope.groupId + '&userId=' + $rootScope.user.Id + '&draftId=' + draftId)
                              .then(function (response) {
                                  closeOnConfirm: false
                                 // swal("deleted");
                                  // $scope.lstdraftmessage = response.data;
                                  $scope.date(response.data);
                              }, function (reason) {
                                  $scope.error = reason.data;
                              });
	          	        
        }
        


        $scope.fetchdraftmessage = function () {
            if ($rootScope.draftDelete != null) {
                                         $scope.deleteafterschdu($rootScope.draft_id);
                                     }
            //codes to load  draft messages start
            $http.get(apiDomain + '/api/DraftMessage/GetAllUserDraftMessages?groupId=' + $rootScope.groupId+'&userId='+$rootScope.user.Id)
                          .then(function (response) {
                              // $rootScope.lstdraftmessage = response.data;
                              if (response.data != "") {
                                  $scope.lastreach = true;
                                  $scope.date(response.data);
                              } else {
                                  $scope.lastreach = true;
                                  $scope.nomessages = true;
                                  $rootScope.lstdraftmessage = "";

                              }
                          }, function (reason) {
                              $scope.error = reason.data;
                          });
            // end codes to load draft messages
        }
        $scope.fetchdraftmessage();


        $scope.getProperURL = function (obj) {
         
            if (obj.includes("wwwroot\\")) {
                var img = obj.split("wwwroot\\")[1];
                return apiDomain + "/api/Media/Get?id=" + img;
            }
            else {
                return obj;
            }
        };


        $scope.editdraft = function (message, draftId)
        {
          
            $rootScope.draftmessgae = message;
            $rootScope.draftId = draftId;
            $('#EditDraftModal').openModal();
            document.getElementById('editdraftScheduleMsg').value = message;
            //$scope.modalinstance = $modal.open({
            //    templateUrl: 'editdraftModalContent.html',
            //    controller: 'DraftMessageController',
            //    scope: $scope
            //});
        }





        $scope.scheduledraft = function (schedulemessage)
        {
            var x;
          
            $rootScope.schedulemessage = schedulemessage;
           // console.log("schedulemsg",$rootScope.schedulemessage);
            $rootScope.draftpost = true;
            $rootScope.draft_id = schedulemessage.id;
            // window.location.href = "#/schedulemsg";
            $state.go('schedulemessage');
            
        }


        $scope.date = function (parm) {

            for (var i = 0; i < parm.length; i++) {
                var date = moment(parm[i].scheduleTime);
                var newdate = date.toString();
                var splitdate = newdate.split(" ");
                date = splitdate[0] + " " + splitdate[1] + " " + splitdate[2] + " " + splitdate[3];
                parm[i].scheduleTime = date;
            }
            $rootScope.lstdraftmessage = parm;

        }


        //$scope.closeModal = function () {
        //    $scope.modalinstance.dismiss('cancel');
        //}
        $scope.saveditdraft = function () {
            var message = $('#editdraftScheduleMsg').val();
            
            //For taking special character start
            var updatedmessage = "";
            var postdata = message.split("\n");
            for (var i = 0; i < postdata.length; i++) {
                updatedmessage = updatedmessage + "<br>" + postdata[i];
            }
            updatedmessage = updatedmessage.replace(/#+/g, 'hhh');
            updatedmessage = updatedmessage.replace(/&+/g, 'nnn');
            updatedmessage = updatedmessage.replace("+", 'ppp');
            updatedmessage = updatedmessage.replace("-+", 'jjj');
            message = updatedmessage;
            //End

            $http.post(apiDomain + '/api/DraftMessage/EditDraftMessage?groupId=' + $rootScope.groupId + '&userId=' + $rootScope.user.Id + '&draftId=' + $rootScope.draftId + '&message=' + message)
                                  .then(function (response) {
                                     // $scope.modalinstance.dismiss('cancel');
                                      //$rootScope.lstdraftmessage = response.data;
                                      $scope.date(response.data);
                                  }, function (reason) {
                                      $scope.error = reason.data;
                                  });
            // end codes to delete draft messages

        }


    });
})
.filter('Url', function ($sce) {
    return function (Url) {
        return $sce.trustAsResourceUrl(Url);
    };
});



