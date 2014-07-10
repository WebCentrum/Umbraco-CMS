
/*********************************************************************************************************/
/* spectrum color picker directive */
/*********************************************************************************************************/

angular.module('spectrumcolorpicker', [])
  .directive('spectrum', function () {
      return {
          restrict: 'E',
          transclude: true,
          scope: {
              colorselected: '='
          },
          link: function (scope, $element) {

              var initColor;

              $element.find("input").spectrum({
                  color: scope.colorselected,
                  preferredFormat: "rgb",
                  showAlpha: true,
                  showInput: true,
                  change: function (color) {
                      scope.colorselected = color.toRgbString();
                      scope.$apply();
                  },
                  move: function (color) {
                      scope.colorselected = color.toRgbString();
                      scope.$apply();
                  },
                  beforeShow: function (color) {
                      initColor = angular.copy(scope.colorselected);
                      $(this).spectrum("container").find(".sp-cancel").click(function (e) {
                          scope.colorselected = initColor;
                          scope.$apply();
                      });
                  },

              });

              scope.$watch('colorselected', function () {
                  $element.find("input").spectrum("set", scope.colorselected);
              }, true);

          },
          template:
          '<div><input type=\'text\' ng-model=\'colorselected\' /></div>',
          replace: true
      };
  })