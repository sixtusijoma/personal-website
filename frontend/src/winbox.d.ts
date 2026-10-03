declare module "winbox/src/js/winbox" {
  interface WinBoxControl {
    index?: number
    class?: string
    image?: string
    click?: (this: HTMLElement, event: Event, winbox: WinBox) => void
  }

  interface WinBoxParams {
    index?: number
    id?: string
    root?: Node
    template?: Node
    class?: string | string[]

    title?: string
    background?: string
    border?: string | number
    header?: number
    icon?: string | false

    modal?: boolean
    max?: boolean
    min?: boolean
    hidden?: boolean

    width?: string | number
    height?: string | number
    minwidth?: string | number
    minheight?: string | number
    maxwidth?: string | number
    maxheight?: string | number
    autosize?: boolean

    x?: "center" | "right" | number | string
    y?: "center" | "bottom" | number | string

    top?: string | number
    right?: string | number
    bottom?: string | number
    left?: string | number

    url?: string | false
    mount?: Node | false
    html?: string
    overflow?: boolean

    onload?: (this: WinBox) => void

    oncreate?: (this: WinBox, options: WinBoxParams) => void
    onshow?: (this: WinBox) => void
    onhide?: (this: WinBox) => void
    onfocus?: (this: WinBox) => void
    onblur?: (this: WinBox) => void
    onresize?: (this: WinBox, width: number, height: number) => void
    onmove?: (this: WinBox, x: number, y: number) => void
    onclose?: (this: WinBox, force?: boolean) => boolean | void
    onfullscreen?: (this: WinBox) => void
    onmaximize?: (this: WinBox) => void
    onminimize?: (this: WinBox) => void
    onrestore?: (this: WinBox) => void
  }

  interface WinBox {
    id: string
    dom: HTMLElement
    window: HTMLElement
    body: HTMLElement
    header: number
    title: string

    x: number
    y: number
    width: number
    height: number
    minwidth: number
    minheight: number
    maxwidth: number
    maxheight: number
    top: number
    right: number
    bottom: number
    left: number
    index: number

    overflow: boolean
    min: boolean
    max: boolean
    full: boolean
    hidden: boolean
    focused: boolean

    onclose: (force?: boolean) => boolean | void
    onfocus: () => void
    onblur: () => void
    onmove: (x: number, y: number) => void
    onresize: (width: number, height: number) => void
    onfullscreen: () => void
    onmaximize: () => void
    onminimize: () => void
    onrestore: () => void
    onhide: () => void
    onshow: () => void

    show(state?: boolean): WinBox
    hide(state?: boolean): WinBox
    focus(state?: boolean): WinBox
    blur(state?: boolean): WinBox
    minimize(state?: boolean): WinBox
    maximize(state?: boolean): WinBox
    fullscreen(state?: boolean): WinBox
    restore(): WinBox
    close(force?: boolean): boolean | undefined
    move(x?: WinBoxParams["x"], y?: WinBoxParams["y"], skipUpdate?: boolean): WinBox
    resize(width?: WinBoxParams["width"], height?: WinBoxParams["height"], skipUpdate?: boolean): WinBox
    setTitle(title: string): WinBox
    setIcon(src: string): WinBox
    setBackground(background: string): WinBox
    setUrl(url: string, onload?: () => void): WinBox
    mount(src?: Node): WinBox
    unmount(dest?: Node): WinBox
    addClass(className: string): WinBox
    removeClass(className: string): WinBox
    hasClass(className: string): boolean
    toggleClass(className: string): WinBox
    addControl(control: WinBoxControl): WinBox
    removeControl(control: string | WinBoxControl): WinBox
  }

  interface WinBoxConstructor {
    new (title: string, params?: WinBoxParams): WinBox
    new (params: WinBoxParams): WinBox
  }

  const WinBox: WinBoxConstructor
  export default WinBox
}
