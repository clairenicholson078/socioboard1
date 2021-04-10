'use strict';

SocioboardApp.controller('YoutubeFeedsController', function ($rootScope, $scope, $http, $timeout, $stateParams, apiDomain, domain) {
    //alert('helo');
    $scope.$on('$viewContentLoaded', function() {   
        youtubefeeds();
        $('#tags').tagsInput();
        $scope.preloadmore = true;
        $scope.SorttTxtt = 'Popular';
        $scope.boxload = function () {
            $('.collapsible').collapsible();
        }

        var currentVideoId = "";
        $scope.LoadVideos = function () {
            //codes to load videos
            $http.get(apiDomain + '/api/Google/GetYTVideos?ChannelId=' + $stateParams.profileId + '&sortType=none')
                              .then(function (response) {
                                  if (response.data == "") {
                                      $scope.nodata = true;
                                      $scope.preloadmorevideos = true;
                                      $scope.dropCalled = true;
                                  }
                                  else {
                                      $scope.lstYtFeeds = response.data;
                                      //$scope.reloadFeeds();
                                      $scope.preloadmorevideos = true;
                                      $scope.dropCalled = true;
                                      setTimeout(function () {
                                          $scope.callDropmenu();
                                      }, 1000);
                                      setTimeout(function () {
                                          onYouTubeIframeAPIReady();
                                      }, 15000);
                                  }
                              }, function (reason) {
                                  $scope.error = reason.data;
                              });
            // end codes to load videos

        }
        $scope.LoadVideos();

        $scope.ReLoadingTopFeeds = function () {
            $scope.preloadmorevideos = false;
            $scope.lstYtFeeds = null;
            $scope.SorttTxtt = 'Popular';
            $scope.LoadVideos();
        }

          //$scope.reloadFeeds = function () {
          //    setTimeout(function () { $scope.LoadVideos(); }, 10000);
          //}

        $scope.videomodal = function (vdoId) {
         
            $scope.lstYtComments = "";
            $scope.preloadmorecomments = false;
            $scope.fetchComments(vdoId);
            
        }

        $scope.sortSearch = function (sortType, txtt) {
            $scope.preloadmorevideos = false;
            $scope.lstYtFeeds = null;
            $scope.SorttTxtt = txtt;

            //codes to load  recent Feeds
            $http.get(apiDomain + '/api/Google/GetYTVideos?ChannelId=' + $stateParams.profileId + '&sortType=' + sortType)
                          .then(function (response) {
                              $scope.lstYtFeeds = response.data;
                              $scope.preloadmorevideos = true;
                              $scope.dropCalled = true;
                              setTimeout(function () {
                                  $scope.callDropmenu();
                              }, 1000);
                              setTimeout(function () {
                                  onYouTubeIframeAPIReady();
                              }, 15000);

                          }, function (reason) {
                              $scope.error = reason.data;
                          });
            // end codes to load  recent Feeds
        }

        $scope.fetchComments = function (vidoId) {
        
            $scope.commentsending = 'hide';
            $scope.sendicon = 'show';
            //codes to load Comments
            $http.get(apiDomain + '/api/Google/GetYtVdoComments?VideoId=' + vidoId)
                              .then(function (response) {
                                  $scope.lstYtComments = response.data;
                                  $scope.preloadmorecomments = true;
                                 
                              }, function (reason) {
                                  $scope.error = reason.data;
                              });
            // end codes to load Comments

        }


        $scope.postComments = function (videoid) {
           
            //codes to post Comments
            var commentText = $('#compose_' + videoid).val();
            var commentencoded = encodeURIComponent(commentText);
            if (/\S/.test(commentText)) {
                $scope.sendicon = 'hide';
                $scope.commentsending = 'show';
                $http.post(apiDomain + '/api/Google/PostCommentsYoutube?channelId=' + $stateParams.profileId + '&videoId=' + videoid + '&commentText=' + commentencoded)
                                  .then(function (response) {
                                     
                                      $scope.commentsending = 'hide';
                                      $scope.sendicon = 'show';
                                      swal("Posted","Comment posted successfully","success");
                                      $('#compose_' + videoid).val('');
                                  }, function (reason) {
                                      $scope.error = reason.data;
                                  });

            }
            else
            {
                swal("Please enter text to post comment");
            }
            // end codes to post Comments

        }

        $scope.callDropmenu = function () {
            $('.dropdown-button').dropdown({
                inDuration: 300,
                outDuration: 225,
                constrain_width: false, // Does not change width of dropdown to that of the activator
                hover: true, // Activate on hover
                gutter: 0, // Spacing from edge
                belowOrigin: false, // Displays dropdown below the button
                alignment: 'right' // Displays dropdown with edge aligned to the left of button
            });
        }

        $scope.uploadvideo = function () {
          
            var formData = new FormData();
            var title = $('#youyube_title').val();
            var descrip = $('#youyube_desc').val();
            var category = $('#categry_vdo').val();
            var status = $('#stats_vdo').val();
            var files = $('#input-file-now').val();
            var fileExt = files.split('.');
            var extOfFile = (fileExt[fileExt.length - 1]);

            //var updatedmessage = "";
            //var updatetitle = "";

            //var postdata = title.split("\n");//newComment
            //for (var i = 0; i < postdata.length; i++) {
            //    updatetitle = updatetitle + "<br>" + postdata[i];
            //}
            //updatetitle = updatetitle.replace(/#+/g, 'hhh');
            //updatetitle = updatetitle.replace(/&+/g, 'nnn');
            //updatetitle = updatetitle.replace("+", 'ppp');
            //updatetitle = updatetitle.replace("-+", 'jjj');
            //title = updatetitle;

            //var postdata = descrip.split("\n");//newComment
            //for (var i = 0; i < postdata.length; i++) {
            //    updatedmessage = updatedmessage + "<br>" + postdata[i];
            //}
            //updatedmessage = updatedmessage.replace(/#+/g, 'hhh');
            //updatedmessage = updatedmessage.replace(/&+/g, 'nnn');
            //updatedmessage = updatedmessage.replace("+", 'ppp');
            //updatedmessage = updatedmessage.replace("-+", 'jjj');
            //descrip = updatedmessage;



            var fileIsSupported = false;
            if (extOfFile == "webm" || extOfFile == "mkv" || extOfFile == "vob" || extOfFile == "flv" || extOfFile == "ogv" || extOfFile == "ogg" || extOfFile == "drc" || extOfFile == "gif" || extOfFile == "gifv" || extOfFile == "mng" || extOfFile == "avi" || extOfFile == "mov" || extOfFile == "qt" || extOfFile == "wmv" || extOfFile == "yuv" || extOfFile == "rm" || extOfFile == "rmvb" || extOfFile == "asf" || extOfFile == "amv" || extOfFile == "mp4" || extOfFile == "m4p" || extOfFile == "m4v" || extOfFile == "mpg" || extOfFile == "mp2" || extOfFile == "mpeg" || extOfFile == "mpe" || extOfFile == "mpv" || extOfFile == "mpg" || extOfFile == "mpeg" || extOfFile == "m2v" || extOfFile == "m2v" || extOfFile == "m4v" || extOfFile == "svi" || extOfFile == "3gp" || extOfFile == "3g2" || extOfFile == "roq" || extOfFile == "nsv" || extOfFile == "flv" || extOfFile == "f4v" || extOfFile == "f4p" || extOfFile == "f4a" || extOfFile == "f4b") {
                fileIsSupported = true;
            }

            if (/\S/.test(title)) {
                if (/\S/.test(descrip)) {
                    if (category != null) {
                        if (fileIsSupported) {
                            if (status != null) {
                                $scope.preloadmore = false;
                                formData.append('files', $("#input-file-now").get(0).files[0]);
                                $http({
                                    method: 'POST',
                                    url: apiDomain + '/api/Google/uploadyoutube?channelid=' + $stateParams.profileId + '&title=' + title + '&descrip=' + descrip + '&category=' + category + '&status=' + status,
                                    data: formData,
                                    headers: {
                                        'Content-Type': undefined
                                    },
                                    transformRequest: angular.identity,
                                }).then(function (response) {
                                    if (response.data == "Posted") {
                                        $('#ComposePostModal').closeModal();
                                        swal({
                                            title: "Video will Upload Soon",
                                            imageUrl: 'http://img.fobito.com/applications/youtube-kids_android.png?w=128',
                                            timer: 2000,
                                            showConfirmButton: false
                                        });
                                        $('#ComposePostModal').closeModal();
                                        window.location.reload();
                                    }
                                    else {
                                        swal("Error!", "Issue while uploading video", "error");
                                    }

                                }, function (reason) {
                                   
                                });
                            }
                        else {
                            swal("Please choose a status video");
                        }
                    }
                    else {
                        swal("Please choose a correct video format");
                    }
                    }
                    else {
                        swal("Please choose category of video");
                    }
                }
                else {
                    swal("Please enter description of video");
                }
            }
            else {
                swal("Please enter title of video")
            }
        }
    
    });
})
.filter('youtubeEmbedUrl', function ($sce) {
        return function (videoId) {
            return $sce.trustAsResourceUrl('https://www.youtube.com/embed/' + videoId + '?rel=0&wmode=Opaque&enablejsapi=1;showinfo=0;controls=1');
        };
});