/**
 * @property {number} sheetX
 * @property {number} sheetY
 */
class SheetPointerEventArgs {
    sheetX;
    sheetY;
    pageX;
    pageY;
    row;
    col;
    altKey;
    ctrlKey;
    shiftKey;
    metaKey;
}

class PointerInputService {
    pointerEnterCallbackName;
    pointerDoubleClickCallbackName;

    constructor(sheetElement, dotnetHelper) {
        this.dotnetHelper = dotnetHelper;
        this.sheetElement = sheetElement;
        this.currentRow = -1
        this.currentCol = -1
    }

    registerPointerEvents(pointerUpCallbackName, pointerDownCallbackName, pointerMoveCallbackName, pointerEnterCallbackName, pointerDoubleClickCallbackName) {
        this.pointerUpCallbackName = pointerUpCallbackName;
        this.pointerDownCallbackName = pointerDownCallbackName;
        this.pointerMoveCallbackName = pointerMoveCallbackName;
        this.pointerEnterCallbackName = pointerEnterCallbackName;
        this.pointerDoubleClickCallbackName = pointerDoubleClickCallbackName;

        this.sheetElement.addEventListener('pointerup', this.onPointerUp.bind(this));
        this.sheetElement.addEventListener('pointerdown', this.onPointerDown.bind(this));
        this.sheetElement.addEventListener('dblclick', this.onDoubleClick.bind(this));
        this.sheetElement.addEventListener('pointermove', this.onPointerMove.bind(this));
    }

    onPointerUp(e) {
        let args = this.getSheetPointerEventArgs(e)
        if (!args)
            return

        this.dotnetHelper.invokeMethodAsync(this.pointerUpCallbackName, args);
    }

    onPointerDown(e) {
        let args = this.getSheetPointerEventArgs(e)
        if (!args)
            return
        this.dotnetHelper.invokeMethodAsync(this.pointerDownCallbackName, args);
    }

    initializeSheet(sheetElement, scrollContainer, dotnetHelper) {
        this.sheetElement = sheetElement;
        this.scrollContainer = scrollContainer;
        // 이후 scrollContainer를 기준으로 scrollBy 실행
    }

    onPointerMove(e) {
        let args = this.getSheetPointerEventArgs(e)
        if (!args)
            return

        if (args.row !== this.currentRow || args.col !== this.currentCol) {
            this.onCellEnter(args)
        }

        // 🔽 자동 스크롤 추가
        const bounds = this.sheetElement.getBoundingClientRect();
        const margin = 30;
        const scrollSpeed = 10;

        let scrollX = 0;
        let scrollY = 0;

        if (e.clientY < bounds.top + margin) scrollY = -scrollSpeed;
        else if (e.clientY > bounds.bottom - margin) scrollY = scrollSpeed;

        if (e.clientX < bounds.left + margin) scrollX = -scrollSpeed;
        else if (e.clientX > bounds.right - margin) scrollX = scrollSpeed;

        if (scrollX !== 0 || scrollY !== 0) {
            this.sheetElement.scrollBy(scrollX, scrollY);
        }

        this.currentRow = args.row
        this.currentCol = args.col

        this.dotnetHelper.invokeMethodAsync(this.pointerMoveCallbackName, args);
    }

    onDoubleClick(e) {
        let args = this.getSheetPointerEventArgs(e)
        if (!args)
            return

        this.dotnetHelper.invokeMethodAsync(this.pointerDoubleClickCallbackName, args);
    }

    onCellEnter(args) {
        if (!args)
            return

        this.dotnetHelper.invokeMethodAsync(this.pointerEnterCallbackName, args);
    }

    dispose() {
        this.sheetElement.removeEventListener('pointerup', this.onPointerUp);
        this.sheetElement.removeEventListener('pointerdown', this.onPointerDown);
        window.removeEventListener('pointermove', this.onPointerMove);
        this.sheetElement.removeEventListener('dblclick', this.onDoubleClick);
    }


    /**
     * @param {MouseEvent} e
     * @returns {SheetPointerEventArgs}
     */
    getSheetPointerEventArgs(e) {
        let rect = this.sheetElement.getBoundingClientRect();
        let x = e.clientX - rect.x;
        let y = e.clientY - rect.y;
        let targetClassList = e.target.classList;
        let row, col = -1
        let cell = e.target.closest('.sheet-cell')

        if (!cell)
            return null

        if (cell && cell.dataset.row && cell.dataset.col) {
            row = parseInt(cell.dataset.row)
            col = parseInt(cell.dataset.col)
        }

        return {
            sheetX: x,
            sheetY: y,
            pageX: e.pageX,
            pageY: e.pageY,
            row: row,
            col: col,
            altKey: e.altKey,
            ctrlKey: e.ctrlKey,
            metaKey: e.metaKey,
            shiftKey: e.shiftKey,
            mouseButton: e.button
        };
    }

}

export function getInputService(sheetElement, dotnetHelper) {
    return new PointerInputService(sheetElement, dotnetHelper);
}