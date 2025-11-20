/**
 * Polyfill for ReactDOM.findDOMNode which was removed in React 19
 * This is needed for react-quill@2.0.0 compatibility with React 19
 *
 * The findDOMNode function finds the actual DOM node backing a component instance.
 * It's deprecated but some libraries like react-quill still use it internally.
 */

import ReactDOM from 'react-dom';

// Check if findDOMNode exists, if not add a polyfill
if (!ReactDOM.findDOMNode) {
  /**
   * Simple implementation of findDOMNode for React 19 compatibility
   * This searches the DOM tree to find the element associated with a component ref
   */
  ReactDOM.findDOMNode = function (component: any): Element | Text | null {
    if (component == null) {
      return null;
    }

    // If it's already a DOM element, return it
    if (component.nodeType) {
      return component;
    }

    // If it has _internalRoot (React 19 fiber structure), traverse to find DOM node
    if (component._reactInternalFiber) {
      let fiber = component._reactInternalFiber;

      // Traverse through the fiber tree to find the DOM node
      while (fiber) {
        if (fiber.stateNode instanceof HTMLElement || fiber.stateNode instanceof Text) {
          return fiber.stateNode;
        }
        fiber = fiber.child;
      }
    }

    // If it has _internalRoot (newer React versions)
    if (component._internalRoot) {
      let fiber = component._internalRoot.current;

      while (fiber) {
        if (fiber.stateNode instanceof HTMLElement || fiber.stateNode instanceof Text) {
          return fiber.stateNode;
        }
        fiber = fiber.child;
      }
    }

    // Fallback: if component has a DOM ref property
    if (component.ref && typeof component.ref === 'object') {
      return component.ref.current;
    }

    return null;
  };
}

export {};
