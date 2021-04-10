﻿'use strict';

SocioboardApp.controller('YoutubeInboxController', function ($rootScope, $scope, $http, $timeout, $mdpDatePicker,apiDomain, $mdpTimePicker, $stateParams) {
    //alert('helo');
    $scope.$on('$viewContentLoaded', function () {

        //============Inbox Codes============
        //Region Inbox
        youtube_inbox();
        $scope.loadAllComments = false;
        $scope.noData = false;
        $scope.VideoShow = 'hide';
        $scope.VideoShowFalse = 'sh';
        $scope.showParentCommentButton = 'show';
        $scope.hideParentCommentButton = 'hide';
        $scope.showChildCommentButton = 'show';
        $scope.hideChildCommentButton = 'hide';
        $scope.loadVideoComments = false;
        $scope.reviewLoaded = 'shhw';//reviewBtn
        $scope.reviewLoading = 'hide';//reviewBtn
        $scope.removingtaskloader = 'hide';//taskremove loader
        $scope.fetchComments = function () {

            $scope.commentsending = 'hide';
            $scope.sendicon = 'show';
            //codes to load Comments
            $http.get(apiDomain + '/api/Google/GetAllYtComments?ChannelId=' + $stateParams.profileid)
                              .then(function (response) {
                                  if (response.data != "") {
                                      $scope.loadAllComments = true;
                                      $scope.lstYtAllComments = response.data;
                                  }
                                  else
                                  {
                                      $scope.loadAllComments = true;
                                      $scope.noData = true;
                                  }
                              }, function (reason) {
                                  $scope.error = reason.data;
                              });
            // end codes to load Comments


        }

        $scope.fetchComments();

        $scope.calledColl = function () {
            $('.collapsible').collapsible();
        }

        $scope.openVideo=function(videoId, commentObject)
        {
            $scope.loadVideoComments = true;
            ////$scope.loadVideoComments = false;
            $scope.singleComment = true;
            $scope.singleCommentShow = commentObject;
            $scope.VideoShow = 'show';
            $scope.VideoShowFalse = 'hide';
            $scope.videoIdTemp = videoId;
            //$scope.fetchParticulerVideoComments(videoId);
        }

        $scope.LoadSingleVideoComments=function(currentVideoIdClicke)
        {
            $scope.singleComment = false;
            $scope.loadVideoComments = false;
            $scope.fetchParticulerVideoComments(currentVideoIdClicke);
        }

        $scope.fetchParticulerVideoComments = function (videoId) {
            $http.get(apiDomain + '/api/Google/GetYtVdoCommentsWithReply?VideoId=' + videoId)
                              .then(function (response) {
                                  $scope.lstYtVideoComments = response.data;
                                  $scope.loadVideoComments = true;
                              }, function (reason) {
                                  $scope.error = reason.data;
                              });
        }



        $scope.postCommentsReply = function (idParentComment, videoId) {

            //codes to post Comments
            var commentText = $('#reply_' + idParentComment).val();
            var commentencoded = encodeURIComponent(commentText);
            if (/\S/.test(commentText)) {
                $scope.showChildCommentButton = 'hide';
                $scope.hideChildCommentButton = 'show';
                $http.post(apiDomain + '/api/Google/PostCommentsYoutubeReply?channelId=' + $stateParams.profileid + '&idParentComment=' + idParentComment + '&commentText=' + commentencoded + '&videoId=' + videoId)
                                  .then(function (response) {
                                      $scope.fetchParticulerVideoComments(videoId);
                                      $scope.fetchComments();
                                      $scope.showChildCommentButton = 'show';
                                      $scope.hideChildCommentButton = 'hide';
                                      swal("Posted", "Reply posted successfully", "success");
                                      $('#reply_' + idParentComment).val('');
                                  }, function (reason) {
                                      $scope.error = reason.data;
                                  });

            }
            else {
                swal("Please enter text for reply");
            }
            // end codes to post Comments

        }


        $scope.postCommentsReplySingleComment = function (idParentComment, videoId) {

            //codes to post Comments
            var commentText = $('#singleCommentReplyText').val();
            var commentencoded = encodeURIComponent(commentText);
            if (/\S/.test(commentText)) {
                $scope.showChildCommentButton = 'hide';
                $scope.hideChildCommentButton = 'show';
                $http.post(apiDomain + '/api/Google/PostCommentsYoutubeReply?channelId=' + $stateParams.profileid + '&idParentComment=' + idParentComment + '&commentText=' + commentencoded + '&videoId=' + videoId)
                                  .then(function (response) {
                                      $scope.showChildCommentButton = 'show';
                                      $scope.hideChildCommentButton = 'hide';
                                      swal("Posted", "Reply posted successfully", "success");
                                      $('#singleCommentReplyText').val('');
                                  }, function (reason) {
                                      $scope.error = reason.data;
                                  });

            }
            else {
                swal("Please enter text for reply");
            }
            // end codes to post Comments

        }

        $scope.postCommentsMain = function (videoid) {

            //codes to post Comments
            var commentText = $('#compose_' + videoid).val();
            var commentencoded = encodeURIComponent(commentText);
            if (/\S/.test(commentText)) {
                $scope.showParentCommentButton = 'hide';
                $scope.hideParentCommentButton = 'show';
                $http.post(apiDomain + '/api/Google/PostCommentsYoutube?channelId=' + $stateParams.profileid + '&videoId=' + videoid + '&commentText=' + commentencoded)
                                  .then(function (response) {
                                      $scope.fetchParticulerVideoComments(videoid);
                                      $scope.fetchComments();
                                      $scope.showParentCommentButton = 'show';
                                      $scope.hideParentCommentButton = 'hide';
                                      swal("Posted", "Comment posted successfully", "success");
                                      $('#compose_' + videoid).val('');
                                  }, function (reason) {
                                      $scope.error = reason.data;
                                  });

            }
            else {
                swal("Please enter text to post comment");
            }
            // end codes to post Comments

        }

        //Youtube each group code (channel wise)
        //$scope.inviteGroupMem = function () {
        //    var emailId = $('#g_invite').val();

        //    $http.post(apiDomain + '/api/YoutubeGroup/InviteGroupMember?channelId=' + $stateParams.profileid + '&userId=' + $rootScope.user.Id + '&emailId=' + emailId)
        //                         .then(function (response) {
        //                             swal('Success');
        //                             $scope.loadGrpMembers();
        //                         }, function (reason) {
        //                             $scope.error = reason.data;
        //                         });
        //}

        //$scope.loadGrpMembers = function () {
        //    $http.get(apiDomain + '/api/YoutubeGroup/GetGroupMember?channelId=' + $stateParams.profileid)
        //                         .then(function (response) {
        //                             $scope.lstGrpMem = response.data;
        //                         }, function (reason) {
        //                             $scope.error = reason.data;
        //                         });
        //}

        //End Youtube each group code (channel wise)


        

        // end task

        $scope.reviewed = function (commentObj, status, commentType) {
            $scope.reviewLoaded = 'hide';
            $scope.reviewLoading = 'shhw';
            if ($rootScope.user.LastName != null) {
                var name = $rootScope.user.FirstName + ' ' + $rootScope.user.LastName;
            }
            else
            {
                var name = $rootScope.user.FirstName;
            }
            $http.post(apiDomain + '/api/Google/ReviewedComment?commentId=' + commentObj.commentId + '&sbUserName=' + name + '&sbUserId=' + $rootScope.user.Id + '&status=' + status + '&commentType=' + commentType)
                                  .then(function (response) {
                                      if (status == true)
                                      {
                                          swal("Reviewed successfully");
                                      }
                                      else
                                      {
                                          swal("Review cancelled");
                                      }
                                      $scope.fetchComments();
                                      $scope.reviewLoaded = 'shhw';
                                      $scope.reviewLoading = 'hide';
                                      $scope.singleCommentShow.sbGrpTaskAssign = false;
                                      $scope.singleCommentShow.review = status;
                                  }, function (reason) {
                                      $scope.error = reason.data;
                                  });
        }

        // task
        $scope.taskOpensModel = function (tempComment) {
            $('#TaskfeedModal').openModal();
            $scope.tempCommentForTask = tempComment;
        }

        $scope.assignedTaskSbMember = function (sbUserObj) {
            
            swal({
                title: "Assign task",
                text: "Task will be assigned to " + sbUserObj.sbUserName,
                type: "info",
                showCancelButton: true,
                closeOnConfirm: false,
                showLoaderOnConfirm: true
            }, function () {
                var commentType = "";
                var commentId = $scope.tempCommentForTask.commentId;
                if ($scope.tempCommentForTask.parentIdforReply == null) {
                    commentType = "main";
                }
                else {
                    commentType = "Reply";
                }
                $http.post(apiDomain + '/api/YoutubeGroup/AssignTask?memberEmail=' + sbUserObj.sbEmailId + '&memberName=' + sbUserObj.sbUserName + '&memberId=' + sbUserObj.userId + '&ownerId=' + sbUserObj.accessSBUserId + '&commentId=' + commentId + '&commentType=' + commentType)
                                      .then(function (response) {
                                          swal("Assigned successfully");
                                          $scope.fetchComments();
                                          $scope.singleCommentShow.sbGrpTaskAssign = true;
                                          $scope.singleCommentShow.sbGrpMemberName = sbUserObj.sbUserName;
                                          $scope.singleCommentShow.review = false;
                                      }, function (reason) {
                                          $scope.error = reason.data;
                                      });
            });



        }

        $scope.RemoveTask = function (commentObj, commentType) {
            $scope.removingtaskloader = 'show';
            $http.post(apiDomain + '/api/YoutubeGroup/RemoveTask?commentId=' + commentObj.commentId + '&commentType=' + commentType)
                                  .then(function (response) {
                                      swal("Task removed");
                                      $scope.fetchComments();
                                      $scope.removingtaskloader = 'hide';
                                      $scope.singleCommentShow.sbGrpTaskAssign = false;
                                  }, function (reason) {
                                      $scope.error = reason.data;
                                  });
        }
        //Endregion Inbox
    });
})
.filter('youtubeEmbedUrl', function ($sce) {
    return function (videoId) {
        return $sce.trustAsResourceUrl('https://www.youtube.com/embed/' + videoId);
    };
});
SocioboardApp.directive('myRepeatTabDirective', function ($timeout) {

    return function (scope, element, attrs) {
        if (scope.$last === true) {
            $timeout(function () {

                $('.collapsible').collapsible({
                    accordion: false // A setting that changes the collapsible behavior to expandable instead of the default accordion style
                });
            });
        }
    };
})
SocioboardApp.directive('myRepeatCommentsDirective', function ($timeout) {

    return function (scope, element, attrs) {
        if (scope.$last === true) {
            $timeout(function () {
                $('ul.tabs').tabs();
            });
        }
    };
})