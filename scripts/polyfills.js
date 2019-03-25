//Polyfills for functions, that not supporting in some browsers.

//polyfill for endsWith function
//Source: https://developer.mozilla.org/ru/docs/Web/JavaScript/Reference/Global_Objects/String/endsWith
if (!String.prototype.endsWith) {
  Object.defineProperty(String.prototype, 'endsWith', {
    value: function(searchString, position) {
      var subjectString = this.toString();
      if (position === undefined || position > subjectString.length) {
        position = subjectString.length;
      }
      position -= searchString.length;
      var lastIndex = subjectString.indexOf(searchString, position);
      return lastIndex !== -1 && lastIndex === position;
    }
  });
}

//polifyll for array.slice()
//Source: https://developer.mozilla.org/ru/docs/Web/JavaScript/Reference/Global_Objects/Array/slice
/**
 * Прокладка для "исправления" отсутствия поддержки в IE < 9 применения slice
 * к хост-объектам вроде NamedNodeMap, NodeList и HTMLCollection
 * (технически, поскольку хост-объекты зависят от реализации,
 * по крайней мере, до ES2015, IE не обязан так работать).
 * Также работает для строк, исправляет поведение IE < 9, позволяя явно указывать undefined
 * вторым аргументом (как в Firefox), и предотвращает ошибки, возникающие при
 * вызове на других объектах DOM.
 */
(function () {
  'use strict';
  var _slice = Array.prototype.slice;

  try {
    // Не может использоваться с элементами DOM в IE < 9
    _slice.call(document.documentElement);
  } catch (e) { // В IE < 9 кидается исключение
    // Функция будет работать для истинных массивов, массивоподобных объектов,
    // NamedNodeMap (атрибуты, сущности, примечания),
    // NodeList (например, getElementsByTagName), HTMLCollection (например, childNodes)
    // и не будет падать на других объектах DOM (как это происходит на элементах DOM в IE < 9)
    Array.prototype.slice = function(begin, end) {
      // IE < 9 будет недоволен аргументом end, равным undefined
      end = (typeof end !== 'undefined') ? end : this.length;

      // Для родных объектов Array мы используем родную функцию slice
      if (Object.prototype.toString.call(this) === '[object Array]') {
        return _slice.call(this, begin, end); 
      }

      // Массивоподобные объекты мы обрабатываем самостоятельно
      var i, cloned = [],
          size, len = this.length;

      // Обрабатываем отрицательное значение begin
      var start = begin || 0;
      start = (start >= 0) ? start: len + start;

      // Обрабатываем отрицательное значение end
      var upTo = (end) ? end : len;
      if (end < 0) {
        upTo = len + end;
      }

      // Фактически ожидаемый размер среза
      size = upTo - start;

      if (size > 0) {
        cloned = new Array(size);
        if (this.charAt) {
          for (i = 0; i < size; i++) {
            cloned[i] = this.charAt(start + i);
          }
        } else {
          for (i = 0; i < size; i++) {
            cloned[i] = this[start + i];
          }
        }
      }

      return cloned;
    };
  }
}());

//The End.